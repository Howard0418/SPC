using Pt1CurrentSchedule;
public class Pt1CurrentScheduleTests
{
 static readonly DateTime Cut=new(2026,11,2,16,0,0,DateTimeKind.Utc);
 static Slice Old()=>new(new(2026,10,1),new(2026,10,2),18,2);
 [Fact]public void AppendKeepsHistoricalVersionsAndGaps(){var old=Old();var p=SchedulePlan.Merge([old],Cut,99,2);Assert.True(p.Changed);Assert.Equal(old,p.Periods[0]);Assert.Equal(new Slice(Cut,null,99,2),p.Periods[1]);Assert.Equal(old.UntilUtc,p.Periods[0].UntilUtc);}
 [Fact]public void SameCurrentSliceIsNoChange(){var p=SchedulePlan.Merge([Old(),new(Cut,null,99,2)],Cut,99,2);Assert.False(p.Changed);Assert.Equal(2,p.Periods.Count);}
 [Fact]public void CannotReplaceDifferentFutureVersion()=>Assert.Throws<InvalidOperationException>(()=>SchedulePlan.Merge([Old(),new(Cut,null,100,2)],Cut,99,2));
 [Fact]public void CannotOverlapHistory()=>Assert.Throws<InvalidOperationException>(()=>SchedulePlan.Merge([new(new(2026,10,1),Cut.AddTicks(1),18,2)],Cut,99,2));
 [Fact]public void AdjacentBoundaryIsAccepted(){var p=SchedulePlan.Merge([new(new(2026,10,1),Cut,18,2)],Cut,99,2);Assert.Equal(2,p.Periods.Count);}
 [Fact]public void MissingFRejected()=>Assert.Throws<InvalidOperationException>(()=>SchedulePlan.Merge([Old()],Cut,99,0));
 [Fact]public void SourceFAddressesNormalizeAndValuesCompare(){var x="[{\"CellAddress\":\"F!$B$3\",\"NumericValue\":0.99},{\"CellAddress\":\"B6\",\"NumericValue\":1}]";var y="[{\"CellAddress\":\"F!B6\",\"NumericValue\":1,\"StandardSolution\":\"EDTA\"},{\"CellAddress\":\"F!B3\",\"NumericValue\":0.990}]";Assert.Equal(SchedulePlan.FSignature(x),SchedulePlan.FSignature(y));}
 [Fact]public void NullFIsNotZero()=>Assert.Throws<InvalidOperationException>(()=>SchedulePlan.FSignature("[{\"CellAddress\":\"F!B3\",\"NumericValue\":null}]"));
 [Fact]public void DuplicateNormalizedFRejected()=>Assert.Throws<InvalidOperationException>(()=>SchedulePlan.FSignature("[{\"CellAddress\":\"B3\",\"NumericValue\":0.99},{\"CellAddress\":\"F!$B$3\",\"NumericValue\":0.99}]"));
 [Fact]public void CorrectCurrentReplacesOnlyReviewedOpenSlice(){var history=Old();var current=new Slice(Cut,null,796,248);var p=SchedulePlan.CorrectCurrent([history,current],Cut,796,248,999);Assert.True(p.Changed);Assert.Equal(history,p.Periods[0]);Assert.Equal(new Slice(Cut,null,999,248),p.Periods[1]);}
 [Fact]public void CorrectCurrentIsIdempotent(){var p=SchedulePlan.CorrectCurrent([Old(),new(Cut,null,999,248)],Cut,796,248,999);Assert.False(p.Changed);}
 [Fact]public void CorrectCurrentRejectsUnexpectedSource()=>Assert.Throws<InvalidOperationException>(()=>SchedulePlan.CorrectCurrent([Old(),new(Cut,null,700,248)],Cut,796,248,999));
 [Fact]public void CorrectCurrentRejectsDifferentBoundaryOrF(){Assert.Throws<InvalidOperationException>(()=>SchedulePlan.CorrectCurrent([Old(),new(Cut.AddTicks(1),null,796,248)],Cut,796,248,999));Assert.Throws<InvalidOperationException>(()=>SchedulePlan.CorrectCurrent([Old(),new(Cut,null,796,247)],Cut,796,248,999));}
}
