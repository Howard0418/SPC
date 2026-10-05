using System.Net;
using MesSpc.Api.Infrastructure.Data;
using MesSpc.Api.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

public class SourceSchemaTests
{
    [Fact]
    public void Migration_OnlyCreatesSourceTableAndUniqueIndex()
    {
        var operations=new MesSpc.Api.Migrations.AddChameleonSourceSettings().UpOperations;
        Assert.Equal(2,operations.Count);
        var table=Assert.Single(operations.OfType<Microsoft.EntityFrameworkCore.Migrations.Operations.CreateTableOperation>());
        Assert.Equal("ChameleonSourceSettings",table.Name);
        Assert.Contains(table.Columns,x=>x.Name=="SourceId"&&x.MaxLength==64);
        var index=Assert.Single(operations.OfType<Microsoft.EntityFrameworkCore.Migrations.Operations.CreateIndexOperation>());
        Assert.True(index.IsUnique);Assert.Equal(new[]{"SourceId"},index.Columns);
    }
    [Theory] [InlineData(false)] [InlineData(true)]
    public async Task SourceTable_EmptyFallsBack_StoredSourceTakesPrecedence(bool stored)
    {
        await using var connection=new SqliteConnection("Data Source=:memory:");await connection.OpenAsync();
        await using var db=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        if(stored){db.ChameleonSourceSettings.Add(new(){SourceId="database",DisplayName="DB",BaseUrl="http://database"});await db.SaveChangesAsync();}
        var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{
            ["Chameleon:Sources:0:SourceId"]="config",["Chameleon:Sources:0:BaseUrl"]="http://config",["Chameleon:Sources:0:Enabled"]="true"}).Build();
        var handler=new RecordingHandler();using var http=new HttpClient(handler);using var cache=new MemoryCache(new MemoryCacheOptions());
        var service=new ChameleonStatusService(http,config,db,cache,NullLogger<ChameleonStatusService>.Instance);
        await service.GetStatusAsync(CancellationToken.None);
        Assert.NotEmpty(handler.Hosts);Assert.All(handler.Hosts,h=>Assert.Equal(stored?"database":"config",h));
    }
    sealed class RecordingHandler:HttpMessageHandler
    {
        public List<string> Hosts {get;}=[];
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct)
        {Hosts.Add(request.RequestUri!.Host);return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));}
    }
}
