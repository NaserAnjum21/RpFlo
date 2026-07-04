using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RpFlo.Application.DTOs;
using RpFlo.Infrastructure.Persistence;
using Testcontainers.MsSql;

namespace RpFlo.Integration.Tests;

public class ApiIntegrationTests : IAsyncLifetime
{
    private readonly MsSqlContainer _mssql = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    private const string RequesterId = "11111111-1111-1111-1111-111111111111";
    private const string RequesterId2 = "22222222-2222-2222-2222-222222222222";
    private const string ManagerId = "33333333-3333-3333-3333-333333333333";
    private const string FinanceId = "44444444-4444-4444-4444-444444444444";

    public async Task InitializeAsync()
    {
        await _mssql.StartAsync();

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                    if (descriptor != null) services.Remove(descriptor);

                    services.AddDbContext<AppDbContext>(options =>
                        options.UseSqlServer(_mssql.GetConnectionString()));
                });
            });

        _client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
        await _mssql.DisposeAsync();
    }

    private void SetUser(string userId)
    {
        _client.DefaultRequestHeaders.Remove("X-User-Id");
        _client.DefaultRequestHeaders.Add("X-User-Id", userId);
    }

    private async Task<Guid> GetSeededRequestIdForRequester(string requesterId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var parsedRequesterId = Guid.Parse(requesterId);

        return await db.ProcurementRequests
            .Where(p => p.RequesterId == parsedRequesterId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => p.Id)
            .FirstAsync();
    }

    [Fact]
    public async Task GetUsers_ShouldReturnSeededUsers()
    {
        var response = await _client.GetAsync("/api/users");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var users = await response.Content.ReadFromJsonAsync<List<UserResponse>>();
        users.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetMetrics_Requester_ShouldReturnOwnScopedMetrics()
    {
        SetUser(RequesterId);

        var response = await _client.GetAsync("/api/procurement/metrics");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var metrics = await response.Content.ReadFromJsonAsync<DashboardMetrics>();
        metrics.Should().NotBeNull();
        metrics!.TotalRequests.Should().Be(12);
        metrics.StatusBreakdown.Sum(item => item.Count).Should().Be(12);
        metrics.DepartmentBreakdown.Sum(item => item.Count).Should().Be(12);
        metrics.RoleMetrics.Should().NotBeNull();
        metrics.RoleMetrics!.PendingMyReview.Should().Be(0);
        metrics.RoleMetrics.ReadyForPo.Should().Be(0);
        metrics.RoleMetrics.TotalValuePending.Should().Be(0);
        metrics.RoleMetrics.MonthlySpendApproved.Should().Be(0);
    }

    [Fact]
    public async Task GetAllProcurements_Requester_ShouldOnlyReturnOwnSeededData()
    {
        SetUser(RequesterId);

        var response = await _client.GetAsync("/api/procurement");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var page = await response.Content.ReadFromJsonAsync<PagedResult<ProcurementListItem>>();
        page.Should().NotBeNull();
        page!.Items.Should().HaveCount(10);
        page.Items.Should().OnlyContain(item => item.RequesterName == "Alice Johnson");
        page.Page.Should().Be(1);
        page.PageSize.Should().Be(10);
        page.TotalItems.Should().Be(12);
        page.TotalPages.Should().Be(2);
    }

    [Fact]
    public async Task GetPagedProcurements_Manager_ShouldReturnFilteredPageMetadata()
    {
        SetUser(ManagerId);

        var response = await _client.GetAsync("/api/procurement?page=2&pageSize=5&filter=pending");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var page = await response.Content.ReadFromJsonAsync<PagedResult<ProcurementListItem>>();
        page.Should().NotBeNull();
        page!.Page.Should().Be(2);
        page.PageSize.Should().Be(5);
        page.TotalItems.Should().Be(6);
        page.TotalPages.Should().Be(2);
        page.Items.Should().HaveCount(1);
        page.Items.Should().OnlyContain(item => item.Status == "Submitted" || item.Status == "ManagerApproved");
    }

    [Fact]
    public async Task GetProcurementById_RequesterAccessingAnotherRequesterRequest_ShouldReturn403()
    {
        var bobRequestId = await GetSeededRequestIdForRequester(RequesterId2);
        SetUser(RequesterId);

        var response = await _client.GetAsync($"/api/procurement/{bobRequestId}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        json.RootElement.GetProperty("code").GetString().Should().Be("Unauthorized.AccessDenied");
    }

    [Fact]
    public async Task GetProcurementById_RequesterAccessingOwnRequest_ShouldReturnRequest()
    {
        var aliceRequestId = await GetSeededRequestIdForRequester(RequesterId);
        SetUser(RequesterId);

        var response = await _client.GetAsync($"/api/procurement/{aliceRequestId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var request = await response.Content.ReadFromJsonAsync<ProcurementResponse>();
        request.Should().NotBeNull();
        request!.Requester.Id.Should().Be(Guid.Parse(RequesterId));
    }

    [Fact]
    public async Task GetProcurement_MissingUserHeader_ShouldReturn401()
    {
        _client.DefaultRequestHeaders.Remove("X-User-Id");

        var response = await _client.GetAsync("/api/procurement");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        json.RootElement.GetProperty("code").GetString().Should().Be("Unauthorized.MissingUserId");
    }

    [Fact]
    public async Task GetPagedPendingProcurements_ShouldReturnRoleScopedPageMetadata()
    {
        SetUser(ManagerId);

        var response = await _client.GetAsync("/api/procurement/pending?page=1&pageSize=2");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var page = await response.Content.ReadFromJsonAsync<PagedResult<ProcurementListItem>>();
        page.Should().NotBeNull();
        page!.Page.Should().Be(1);
        page.PageSize.Should().Be(2);
        page.TotalItems.Should().Be(3);
        page.TotalPages.Should().Be(2);
        page.Items.Should().HaveCount(2);
        page.Items.Should().OnlyContain(item => item.Status == "Submitted");
        page.Items[0].Urgency.Should().Be("Critical");
    }

    [Fact]
    public async Task CreateProcurement_ShouldSucceed()
    {
        SetUser(RequesterId);

        var request = new CreateProcurementRequest(
            "Test Request",
            "Integration test procurement",
            Domain.Enums.Department.Engineering,
            Domain.Enums.Urgency.Medium,
            [new("Test Item", 2, 99.99m)]);

        var response = await _client.PostAsJsonAsync("/api/procurement", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<ProcurementResponse>();
        result.Should().NotBeNull();
        result!.Title.Should().Be("Test Request");
        result.Status.Should().Be("Draft");
        result.LineItems.Should().HaveCount(1);
        result.TotalAmount.Should().Be(199.98m);
    }

    [Fact]
    public async Task FullWorkflow_CreateSubmitApproveIssuePO()
    {
        SetUser(RequesterId);

        var request = new CreateProcurementRequest(
            "Workflow Test",
            "Full workflow test",
            Domain.Enums.Department.Engineering,
            Domain.Enums.Urgency.High,
            [new("Server", 1, 5000m)]);

        var createResponse = await _client.PostAsJsonAsync("/api/procurement", request);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<ProcurementResponse>();
        var id = created!.Id;

        var submitResponse = await _client.PostAsync($"/api/procurement/{id}/submit", null);
        submitResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        SetUser(ManagerId);
        var approveResponse = await _client.PostAsJsonAsync(
            $"/api/procurement/{id}/approve/manager",
            new ApprovalRequest("Approved by manager"));
        approveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        SetUser(FinanceId);
        var financeApproveResponse = await _client.PostAsJsonAsync(
            $"/api/procurement/{id}/approve/finance",
            new ApprovalRequest("Finance approved"));
        financeApproveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var poResponse = await _client.PostAsync($"/api/procurement/{id}/issue-po", null);
        poResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var final = await poResponse.Content.ReadFromJsonAsync<ProcurementResponse>();
        final!.Status.Should().Be("PurchaseOrderIssued");
        final.PoNumber.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RejectAndRevise_ShouldWork()
    {
        SetUser(RequesterId);

        var request = new CreateProcurementRequest(
            "Reject Test",
            "Test rejection flow",
            Domain.Enums.Department.Marketing,
            Domain.Enums.Urgency.Low,
            [new("Brochures", 100, 5m)]);

        var created = await (await _client.PostAsJsonAsync("/api/procurement", request))
            .Content.ReadFromJsonAsync<ProcurementResponse>();
        var id = created!.Id;

        await _client.PostAsync($"/api/procurement/{id}/submit", null);

        SetUser(ManagerId);
        var rejectResponse = await _client.PostAsJsonAsync(
            $"/api/procurement/{id}/reject/manager",
            new RejectionRequest("Needs vendor quotes"));
        rejectResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        SetUser(RequesterId);
        var reviseResponse = await _client.PostAsync($"/api/procurement/{id}/revise", null);
        reviseResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var revised = await reviseResponse.Content.ReadFromJsonAsync<ProcurementResponse>();
        revised!.Status.Should().Be("Draft");
    }

    [Fact]
    public async Task UnauthorizedApproval_ShouldReturn403()
    {
        SetUser(RequesterId);

        var request = new CreateProcurementRequest(
            "Auth Test",
            "Test authorization",
            Domain.Enums.Department.Engineering,
            Domain.Enums.Urgency.Low,
            [new("Item", 1, 10m)]);

        var created = await (await _client.PostAsJsonAsync("/api/procurement", request))
            .Content.ReadFromJsonAsync<ProcurementResponse>();
        var id = created!.Id;

        await _client.PostAsync($"/api/procurement/{id}/submit", null);

        var approveResponse = await _client.PostAsJsonAsync(
            $"/api/procurement/{id}/approve/manager",
            new ApprovalRequest(null));
        approveResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AddComment_ShouldSucceed()
    {
        SetUser(RequesterId);

        var request = new CreateProcurementRequest(
            "Comment Test",
            "Test comments",
            Domain.Enums.Department.Engineering,
            Domain.Enums.Urgency.Low,
            [new("Item", 1, 10m)]);

        var created = await (await _client.PostAsJsonAsync("/api/procurement", request))
            .Content.ReadFromJsonAsync<ProcurementResponse>();

        var commentResponse = await _client.PostAsJsonAsync(
            $"/api/procurement/{created!.Id}/comments",
            new AddCommentRequest("Need this ASAP"));
        commentResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var comment = await commentResponse.Content.ReadFromJsonAsync<CommentResponse>();
        comment.Should().NotBeNull();
        comment!.Text.Should().Be("Need this ASAP");
        comment.UserId.Should().Be(Guid.Parse(RequesterId));
    }

    [Fact]
    public async Task AddComment_RequesterAccessingAnotherRequesterRequest_ShouldReturn403()
    {
        var bobRequestId = await GetSeededRequestIdForRequester(RequesterId2);
        SetUser(RequesterId);

        var response = await _client.PostAsJsonAsync(
            $"/api/procurement/{bobRequestId}/comments",
            new AddCommentRequest("This should not be accepted"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        json.RootElement.GetProperty("code").GetString().Should().Be("Unauthorized.AccessDenied");
    }

    [Fact]
    public async Task AddLineItems_InvalidData_ShouldReturn400()
    {
        SetUser(RequesterId);

        var request = new CreateProcurementRequest(
            "Add Item Validation Test",
            "Test add item validation",
            Domain.Enums.Department.Engineering,
            Domain.Enums.Urgency.Low,
            [new("Item", 1, 10m)]);

        var created = await (await _client.PostAsJsonAsync("/api/procurement", request))
            .Content.ReadFromJsonAsync<ProcurementResponse>();

        var response = await _client.PostAsJsonAsync(
            $"/api/procurement/{created!.Id}/line-items",
            new AddLineItemsRequest([new("Bad Item", -1, 10m)]));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task MarkNotificationRead_ForAnotherUser_ShouldReturn404()
    {
        SetUser(RequesterId);

        var request = new CreateProcurementRequest(
            "Notification Ownership Test",
            "Test notification ownership",
            Domain.Enums.Department.Engineering,
            Domain.Enums.Urgency.Low,
            [new("Item", 1, 10m)]);

        var created = await (await _client.PostAsJsonAsync("/api/procurement", request))
            .Content.ReadFromJsonAsync<ProcurementResponse>();

        await _client.PostAsync($"/api/procurement/{created!.Id}/submit", null);

        SetUser(ManagerId);
        var managerNotifications = await (await _client.GetAsync("/api/notifications"))
            .Content.ReadFromJsonAsync<List<NotificationResponse>>();

        managerNotifications.Should().NotBeEmpty();

        SetUser(RequesterId2);
        var response = await _client.PostAsync($"/api/notifications/{managerNotifications![0].Id}/read", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ExportCsv_ShouldReturnFile()
    {
        SetUser(RequesterId);

        var response = await _client.GetAsync("/api/export/csv");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");

        var csv = await response.Content.ReadAsStringAsync();
        csv.Should().StartWith("Id,Title,Department,Urgency,Status,TotalAmount,Currency,Requester,CreatedAt,UpdatedAt");
        csv.Should().Contain("Alice Johnson");
        csv.Should().NotContain("Bob Smith");
    }

    [Fact]
    public async Task CreateProcurement_InvalidData_ShouldReturn400()
    {
        SetUser(RequesterId);

        var request = new CreateProcurementRequest(
            "",
            "",
            Domain.Enums.Department.Engineering,
            Domain.Enums.Urgency.Low,
            []);

        var response = await _client.PostAsJsonAsync("/api/procurement", request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        json.RootElement.GetProperty("type").GetString().Should().Be("ValidationError");
        var errors = json.RootElement.GetProperty("errors");
        errors.TryGetProperty("Title", out _).Should().BeTrue();
        errors.TryGetProperty("Description", out _).Should().BeTrue();
        errors.TryGetProperty("LineItems", out _).Should().BeTrue();
    }

    [Fact]
    public async Task CreateProcurement_MissingUserHeader_ShouldReturn401()
    {
        _client.DefaultRequestHeaders.Remove("X-User-Id");

        var request = new CreateProcurementRequest(
            "Missing Header Test",
            "Should be rejected before hitting the controller",
            Domain.Enums.Department.Engineering,
            Domain.Enums.Urgency.Low,
            [new("Item", 1, 10m)]);

        var response = await _client.PostAsJsonAsync("/api/procurement", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        json.RootElement.GetProperty("code").GetString().Should().Be("Unauthorized.MissingUserId");
    }
}
