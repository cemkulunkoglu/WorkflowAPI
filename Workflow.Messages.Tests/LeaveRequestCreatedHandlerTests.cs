using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

using MessagesService.Data;
using MessagesService.Entities;
using MessagesService.Events;
using MessagesService.Interfaces;
using MessagesService.Workers;

public class LeaveRequestCreatedHandlerTests
{
    // 🔥 KRİTİK: InMemory DB store'u tüm scope'lar arasında paylaşmak için
    private static readonly InMemoryDatabaseRoot _dbRoot = new();

    private static ServiceProvider BuildProvider(
        Mock<IApproverEmailLookup> lookupMock,
        string dbName,
        Dictionary<string, string?>? config = null)
    {
        var services = new ServiceCollection();

        services.AddLogging(x => x.AddDebug().SetMinimumLevel(LogLevel.Information));

        // 🔥 KRİTİK: aynı dbName + aynı root => aynı store
        services.AddDbContext<MessagesDbContext>(opt =>
            opt.UseInMemoryDatabase(dbName, _dbRoot));

        var cfgDict = new Dictionary<string, string?>
        {
            ["LeaveRequest:SecondApproverMinDays"] = "4",
            ["LeaveRequest:TemplateName"] = "LEAVE_REQUEST_CREATED",
            ["Frontend:BaseUrl"] = "http://localhost:5173",
            ["Smtp:From"] = "noreply@workflow.local"
        };

        if (config != null)
            foreach (var kv in config) cfgDict[kv.Key] = kv.Value;

        IConfiguration cfg = new ConfigurationBuilder()
            .AddInMemoryCollection(cfgDict)
            .Build();

        services.AddSingleton(cfg);
        services.AddSingleton(lookupMock.Object);

        // Handler singleton olabilir; içeride zaten scope açıyor
        services.AddSingleton<ILeaveRequestCreatedHandler, LeaveRequestCreatedHandler>();

        return services.BuildServiceProvider();
    }

    private static async Task SeedTemplateAsync(ServiceProvider sp)
    {
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MessagesDbContext>();

        await db.Database.EnsureCreatedAsync();

        db.MessageTemplates.Add(new MessageTemplate
        {
            Name = "LEAVE_REQUEST_CREATED",
            IsActive = true,
            Subject = "Yeni İzin Talebi - {user_name}",
            Body = "Merhaba {approver_name}, {user_name} {day_count} gün izin istedi.",
            UiBody = "<b>{user_name}</b> ({day_count} gün) izin istedi."
        });

        await db.SaveChangesAsync();
    }

    private static async Task<List<OutboxMessage>> GetOutboxAsync(ServiceProvider sp)
    {
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MessagesDbContext>();
        return await db.Outbox.ToListAsync();
    }

    [Fact]
    public async Task DayCount_Gte4_Should_SendOnly_ToSecondManager()
    {
        var dbName = "db-" + Guid.NewGuid();

        // Arrange
        var lookup = new Mock<IApproverEmailLookup>();

        lookup.Setup(x => x.GetEmployeePathByEmployeeIdAsync(18, It.IsAny<CancellationToken>()))
              .ReturnsAsync("/5/2/18/");

        lookup.Setup(x => x.GetEmployeeEmailByEmployeeIdAsync(5, It.IsAny<CancellationToken>()))
              .ReturnsAsync("manager2@sirket.com");
        lookup.Setup(x => x.GetEmployeeEmailByEmployeeIdAsync(2, It.IsAny<CancellationToken>()))
              .ReturnsAsync("manager1@sirket.com");

        // (handler bunları çağırmıyor ama dursun)
        lookup.Setup(x => x.GetApproverEmailByEmployeeIdAsync(5, It.IsAny<CancellationToken>()))
              .ReturnsAsync("manager2@sirket.com");
        lookup.Setup(x => x.GetApproverEmailByEmployeeIdAsync(2, It.IsAny<CancellationToken>()))
              .ReturnsAsync("manager1@sirket.com");

        lookup.Setup(x => x.GetEmployeeFullNameByEmployeeIdAsync(18, It.IsAny<CancellationToken>()))
              .ReturnsAsync("Test User");
        lookup.Setup(x => x.GetEmployeeFullNameByEmployeeIdAsync(5, It.IsAny<CancellationToken>()))
              .ReturnsAsync("Manager Two");
        lookup.Setup(x => x.GetEmployeeFullNameByEmployeeIdAsync(2, It.IsAny<CancellationToken>()))
              .ReturnsAsync("Manager One");

        var sp = BuildProvider(lookup, dbName);
        await SeedTemplateAsync(sp);

        var payload = new LeaveRequestCreatedEvent
        {
            LeaveRequestId = 123,
            EmployeeId = 18,
            DayCount = 6,
            StartDate = new DateTime(2026, 1, 10),
            EndDate = new DateTime(2026, 1, 15),
            Reason = "Test"
        };

        // Act
        bool inserted;
        using (var scope = sp.CreateScope())
        {
            var handler = scope.ServiceProvider.GetRequiredService<ILeaveRequestCreatedHandler>();
            inserted = await handler.HandleAsync(payload, CancellationToken.None);
        }

        // Assert
        inserted.Should().BeTrue();

        var outbox = await GetOutboxAsync(sp);
        outbox.Should().HaveCount(1);

        outbox[0].EmailTo.Should().Be("manager2@sirket.com");
        outbox[0].EmployeeToId.Should().Be(5);
        outbox[0].Subject.Should().Contain("Test User");
        outbox[0].Body.Should().Contain("Manager Two").And.Contain("Test User").And.Contain("6");
        outbox[0].UiBody.Should().Contain("Test User").And.Contain("6");
    }

    [Fact]
    public async Task DayCount_Lt4_Should_SendOnly_ToFirstManager()
    {
        var dbName = "db-" + Guid.NewGuid();

        // Arrange
        var lookup = new Mock<IApproverEmailLookup>();

        lookup.Setup(x => x.GetEmployeePathByEmployeeIdAsync(18, It.IsAny<CancellationToken>()))
              .ReturnsAsync("/5/2/18/");

        lookup.Setup(x => x.GetEmployeeEmailByEmployeeIdAsync(5, It.IsAny<CancellationToken>()))
              .ReturnsAsync("manager2@sirket.com");
        lookup.Setup(x => x.GetEmployeeEmailByEmployeeIdAsync(2, It.IsAny<CancellationToken>()))
              .ReturnsAsync("manager1@sirket.com");

        lookup.Setup(x => x.GetApproverEmailByEmployeeIdAsync(5, It.IsAny<CancellationToken>()))
              .ReturnsAsync("manager2@sirket.com");
        lookup.Setup(x => x.GetApproverEmailByEmployeeIdAsync(2, It.IsAny<CancellationToken>()))
              .ReturnsAsync("manager1@sirket.com");

        lookup.Setup(x => x.GetEmployeeFullNameByEmployeeIdAsync(18, It.IsAny<CancellationToken>()))
              .ReturnsAsync("Test User");
        lookup.Setup(x => x.GetEmployeeFullNameByEmployeeIdAsync(5, It.IsAny<CancellationToken>()))
              .ReturnsAsync("Manager Two");
        lookup.Setup(x => x.GetEmployeeFullNameByEmployeeIdAsync(2, It.IsAny<CancellationToken>()))
              .ReturnsAsync("Manager One");

        var sp = BuildProvider(lookup, dbName);
        await SeedTemplateAsync(sp);

        var payload = new LeaveRequestCreatedEvent
        {
            LeaveRequestId = 124,
            EmployeeId = 18,
            DayCount = 2,
            StartDate = new DateTime(2026, 1, 10),
            EndDate = new DateTime(2026, 1, 11),
            Reason = "Test"
        };

        // Act
        bool inserted;
        using (var scope = sp.CreateScope())
        {
            var handler = scope.ServiceProvider.GetRequiredService<ILeaveRequestCreatedHandler>();
            inserted = await handler.HandleAsync(payload, CancellationToken.None);
        }

        // Assert
        inserted.Should().BeTrue();

        var outbox = await GetOutboxAsync(sp);
        outbox.Should().HaveCount(1);

        outbox[0].EmailTo.Should().Be("manager1@sirket.com");
        outbox[0].EmployeeToId.Should().Be(2);
        outbox[0].Body.Should().Contain("Manager One").And.Contain("Test User").And.Contain("2");
    }

    [Fact]
    public async Task Same_LeaveRequest_Should_Not_Insert_Twice_For_Same_Recipient()
    {
        var dbName = "db-" + Guid.NewGuid();

        var lookup = new Mock<IApproverEmailLookup>();
        lookup.Setup(x => x.GetEmployeePathByEmployeeIdAsync(18, It.IsAny<CancellationToken>()))
              .ReturnsAsync("/5/2/18/");

        // 6 gün => manager2 (id=5)
        lookup.Setup(x => x.GetEmployeeEmailByEmployeeIdAsync(5, It.IsAny<CancellationToken>()))
              .ReturnsAsync("manager2@sirket.com");

        lookup.Setup(x => x.GetEmployeeFullNameByEmployeeIdAsync(18, It.IsAny<CancellationToken>()))
              .ReturnsAsync("Test User");
        lookup.Setup(x => x.GetEmployeeFullNameByEmployeeIdAsync(5, It.IsAny<CancellationToken>()))
              .ReturnsAsync("Manager Two");

        var sp = BuildProvider(lookup, dbName);
        await SeedTemplateAsync(sp);

        var payload = new LeaveRequestCreatedEvent
        {
            LeaveRequestId = 999,
            EmployeeId = 18,
            DayCount = 6,
            StartDate = new DateTime(2026, 1, 10),
            EndDate = new DateTime(2026, 1, 15),
            Reason = "Test"
        };

        // 1. çalıştır
        using (var scope = sp.CreateScope())
        {
            var handler = scope.ServiceProvider.GetRequiredService<ILeaveRequestCreatedHandler>();
            (await handler.HandleAsync(payload, CancellationToken.None)).Should().BeTrue();
        }

        // 2. çalıştır (aynı payload)
        using (var scope = sp.CreateScope())
        {
            var handler = scope.ServiceProvider.GetRequiredService<ILeaveRequestCreatedHandler>();
            (await handler.HandleAsync(payload, CancellationToken.None)).Should().BeFalse();
        }

        // Outbox hala 1 olmalı
        var outbox = await GetOutboxAsync(sp);
        outbox.Should().HaveCount(1);
    }

    [Fact]
    public async Task Template_Should_Replace_All_Placeholders()
    {
        var dbName = "db-" + Guid.NewGuid();

        var lookup = new Mock<IApproverEmailLookup>();
        lookup.Setup(x => x.GetEmployeePathByEmployeeIdAsync(18, It.IsAny<CancellationToken>()))
              .ReturnsAsync("/5/2/18/");

        lookup.Setup(x => x.GetEmployeeEmailByEmployeeIdAsync(5, It.IsAny<CancellationToken>()))
              .ReturnsAsync("manager2@sirket.com");

        lookup.Setup(x => x.GetEmployeeFullNameByEmployeeIdAsync(18, It.IsAny<CancellationToken>()))
              .ReturnsAsync("Test User");

        lookup.Setup(x => x.GetEmployeeFullNameByEmployeeIdAsync(5, It.IsAny<CancellationToken>()))
              .ReturnsAsync("Manager Two");

        var sp = BuildProvider(lookup, dbName);

        // Bu test için template’i biraz daha “placeholder dolu” seed edelim
        using (var scope = sp.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MessagesDbContext>();
            await db.Database.EnsureCreatedAsync();

            db.MessageTemplates.Add(new MessageTemplate
            {
                Name = "LEAVE_REQUEST_CREATED",
                IsActive = true,
                Subject = "Yeni İzin Talebi - {user_name}",
                Body = "Merhaba {approver_name}, {user_name} {start_date} - {end_date} ({day_count}) sebep:{reason} id:{leave_request_id}",
                UiBody = "<b>{user_name}</b> {day_count} gün <a href='{approve_url}'>Onayla</a> <a href='{reject_url}'>Reddet</a>"
            });

            await db.SaveChangesAsync();
        }

        var payload = new LeaveRequestCreatedEvent
        {
            LeaveRequestId = 555,
            EmployeeId = 18,
            DayCount = 6,
            StartDate = new DateTime(2026, 1, 10),
            EndDate = new DateTime(2026, 1, 15),
            Reason = "Annual leave"
        };

        using (var scope = sp.CreateScope())
        {
            var handler = scope.ServiceProvider.GetRequiredService<ILeaveRequestCreatedHandler>();
            (await handler.HandleAsync(payload, CancellationToken.None)).Should().BeTrue();
        }

        var outbox = await GetOutboxAsync(sp);
        outbox.Should().HaveCount(1);

        outbox[0].Subject.Should().NotContain("{");
        outbox[0].Body.Should().NotContain("{");
        outbox[0].UiBody.Should().NotContain("{");

        outbox[0].Body.Should().Contain("Test User").And.Contain("Manager Two");
        outbox[0].Body.Should().Contain("10.01.2026").And.Contain("15.01.2026");
        outbox[0].Body.Should().Contain("Annual leave").And.Contain("555");
    }

}
