import argparse
import json
import statistics
from pathlib import Path


def read(path: Path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def close(actual, expected, tolerance=1e-8):
    assert abs(float(actual) - float(expected)) < tolerance, (actual, expected)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--manifest", required=True, type=Path)
    parser.add_argument("--evidence", required=True, type=Path)
    parser.add_argument("--first-run", required=True, type=Path)
    parser.add_argument("--second-run", type=Path)
    args = parser.parse_args()

    reports = read(args.manifest)["reports"]
    database = read(args.evidence / "database-after.json")
    charts = read(args.evidence / "charts.json")
    first = read(args.first_run)

    portal = database["portal"]
    spc = database["spc"]
    headers = database["reports"]
    mappings = database["maps"]
    quarantined = database["quarantined"]

    assert len(reports) == 48
    assert len(portal) == 3650
    assert len(spc) == 3746
    assert len(headers) == 48
    assert len(mappings) == 16
    assert len(charts) == 16
    assert quarantined == []
    assert all(x["SpcSyncStatus"] == "SUCCESS" for x in headers)

    for report in reports:
        date = report["reportDate"]
        line = report["lineCode"]
        ops = 5 if line in ("PT1", "QE1") else 10
        portal_rows = [
            x for x in portal if x["date"] == date and x["line"] == line
        ]
        spc_rows = [x for x in spc if x["date"] == date and x["line"] == line]
        assert len(portal_rows) == len(report["points"])
        assert len(spc_rows) == len(portal_rows) + 2

        for point in report["points"]:
            matches = [
                x
                for x in portal_rows
                if (x["Side"], x["RepeatNo"], x["OpNo"])
                == (point["side"], point["repeatNo"], point["opNo"])
            ]
            assert len(matches) == 1
            row = matches[0]
            close(row["BeforeValue"], point["beforeValue"])
            close(row["AfterValue"], point["afterValue"])
            close(
                row["EtchAmount"],
                point["beforeValue"] - point["afterValue"],
                1e-6,
            )
            close(row["LineSpeed"], report["lineSpeed"])
            assert row["SpcSyncStatus"] == "SUCCESS"

        for code in (
            "ETCH_A_AVG",
            "ETCH_B_AVG",
            "ETCH_RATE",
            "ETCH_LINE_SPEED",
        ):
            rows = sorted(
                [x for x in spc_rows if x["code"] == code],
                key=lambda x: x["SampleNo"],
            )
            if code.endswith("_AVG"):
                side = "A" if code == "ETCH_A_AVG" else "B"
                values = [
                    x["beforeValue"] - x["afterValue"]
                    for x in report["points"]
                    if x["side"] == side
                ]
            else:
                value = (
                    report["lineSpeed"]
                    if code == "ETCH_LINE_SPEED"
                    else statistics.mean(
                        x["beforeValue"] - x["afterValue"]
                        for x in report["points"]
                    )
                    * report["lineSpeed"]
                    / {
                        "PT1": 0.975,
                        "PT2": 0.975,
                        "QE1": 1.944,
                        "QE2": 2.268,
                    }[line]
                )
                values = [value]

            assert len(rows) == len(values)
            for sample, (row, value) in enumerate(zip(rows, values), 1):
                assert row["SampleNo"] == sample
                assert row["SourceReference"] == f"ETCH:{date}:{line}"
                close(row["MeasuredValue"], value)

            chart = next(
                x["chart"]
                for x in charts
                if x["line"] == line and x["code"] == code
            )
            points = chart["chartData"]["points"]
            chart_point = next(
                x for x in points if x["measuredAt"][:10] == date
            )
            assert len(points) == sum(
                x["lineCode"] == line for x in reports
            )
            if code.endswith("_AVG"):
                assert chart["chartType"] == "XBAR_S"
                assert chart_point["n"] == ops * 5
                close(chart_point["xbar"], statistics.mean(values))
                close(chart_point["stdDev"], statistics.stdev(values))
            else:
                close(chart_point["value"], values[0])

    assert len(first) == 48
    assert all(not x["Unchanged"] for x in first)

    replay_unchanged = None
    if args.second_run:
        second = read(args.second_run)
        assert len(second) == 48
        assert all(x["Unchanged"] for x in second)
        first_keys = [
            (
                x["ReportDate"],
                x["LineCode"],
                x["PortalReportId"],
                x["BatchId"],
            )
            for x in first
        ]
        second_keys = [
            (
                x["ReportDate"],
                x["LineCode"],
                x["PortalReportId"],
                x["BatchId"],
            )
            for x in second
        ]
        assert first_keys == second_keys
        replay_unchanged = 48

    summary = {
        "reports": 48,
        "newReports": 48,
        "portalPoints": 3650,
        "spcRows": 3746,
        "mappings": 16,
        "charts": 16,
        "quarantinedRows": 0,
        "rawValuesMatched": True,
        "meansAndSampleSdMatched": True,
        "replayUnchanged": replay_unchanged,
        "groups": {
            line: sum(r["lineCode"] == line for r in reports)
            for line in ["PT1", "PT2", "QE1", "QE2"]
        },
    }
    (args.evidence / "reconciliation.json").write_text(
        json.dumps(summary, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )
    print(json.dumps(summary, ensure_ascii=False))


if __name__ == "__main__":
    main()
