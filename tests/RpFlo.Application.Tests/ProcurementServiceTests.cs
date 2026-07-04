using FluentAssertions;
using NSubstitute;
using RpFlo.Application.DTOs;
using RpFlo.Application.Interfaces;
using RpFlo.Application.Services;
using RpFlo.Domain.Entities;
using RpFlo.Domain.Enums;

namespace RpFlo.Application.Tests;

public class ProcurementServiceTests
{
    private readonly IProcurementRepository _procurementRepo = Substitute.For<IProcurementRepository>();
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly INotificationRepository _notificationRepo = Substitute.For<INotificationRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ProcurementService _service;

    public ProcurementServiceTests()
    {
        _service = new ProcurementService(_procurementRepo, _userRepo, _notificationRepo, _unitOfWork);

        _procurementRepo.AddAsync(Arg.Any<ProcurementRequest>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(call.Arg<ProcurementRequest>()));
        _procurementRepo.UpdateAsync(Arg.Any<ProcurementRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _notificationRepo.AddAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));
    }

    [Fact]
    public async Task CreateAsync_UnknownRequester_ReturnsNotFoundAndDoesNotPersist()
    {
        var requesterId = Guid.NewGuid();
        var request = new CreateProcurementRequest(
            "Laptop refresh",
            "Engineering laptop refresh",
            Department.Engineering,
            Urgency.High,
            [new("MacBook Pro", 2, 2500m)]);
        _userRepo.GetByIdAsync(requesterId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<User?>(null));

        var result = await _service.CreateAsync(request, requesterId);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NotFound.User");
        await _procurementRepo.DidNotReceive()
            .AddAsync(Arg.Any<ProcurementRequest>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive()
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SubmitAsync_DraftRequest_NotifiesManagersAndReturnsSubmittedResponse()
    {
        var requester = CreateUser("Alice Requester", UserRole.Requester);
        var managerA = CreateUser("Manny Manager", UserRole.Manager);
        var managerB = CreateUser("Morgan Manager", UserRole.Manager);
        var procurement = CreateDraftWithItem(requester.Id);

        _procurementRepo.GetByIdAsync(procurement.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ProcurementRequest?>(procurement));
        _userRepo.GetByIdAsync(requester.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<User?>(requester));
        _userRepo.GetByRoleAsync(UserRole.Manager, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<User>>(new List<User> { managerA, managerB }));

        var result = await _service.SubmitAsync(procurement.Id, requester.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("Submitted");
        result.Value.AuditTrail.Should().ContainSingle(a =>
            a.Action == "Submitted" &&
            a.UserId == requester.Id &&
            a.FromStatus == "Draft" &&
            a.ToStatus == "Submitted");
        await _procurementRepo.Received(1)
            .UpdateAsync(procurement, Arg.Any<CancellationToken>());
        await _notificationRepo.Received(1)
            .AddAsync(Arg.Is<Notification>(n =>
                n.UserId == managerA.Id &&
                n.Title == "New Procurement Request" &&
                n.ReferenceId == procurement.Id), Arg.Any<CancellationToken>());
        await _notificationRepo.Received(1)
            .AddAsync(Arg.Is<Notification>(n =>
                n.UserId == managerB.Id &&
                n.Title == "New Procurement Request" &&
                n.ReferenceId == procurement.Id), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1)
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPagedVisibleForUserAsync_Requester_UsesRoleScopedRepository()
    {
        var requester = CreateUser("Alice Requester", UserRole.Requester);
        var procurement = CreateDraftWithItem(requester.Id);
        var query = new ProcurementListPageQuery();
        var page = new PagedResult<ProcurementRequest>([procurement], 1, 10, 1, 1);

        _userRepo.GetByIdAsync(requester.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<User?>(requester));
        _procurementRepo.GetPagedVisibleForUserAsync(requester.Id, requester.Role, query, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(page));
        _userRepo.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<User>>(new List<User> { requester }));

        var result = await _service.GetPagedVisibleForUserAsync(requester.Id, query);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle(item =>
            item.Id == procurement.Id &&
            item.RequesterName == requester.Name);
        await _procurementRepo.Received(1)
            .GetPagedVisibleForUserAsync(requester.Id, requester.Role, query, Arg.Any<CancellationToken>());
        await _procurementRepo.DidNotReceive()
            .GetPagedAsync(Arg.Any<ProcurementListPageQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByIdForUserAsync_RequesterAccessingAnotherRequesterRequest_ReturnsUnauthorized()
    {
        var requester = CreateUser("Alice Requester", UserRole.Requester);
        var otherRequester = CreateUser("Bob Requester", UserRole.Requester);
        var procurement = CreateSubmittedRequest(otherRequester.Id);

        _userRepo.GetByIdAsync(requester.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<User?>(requester));
        _procurementRepo.GetByIdAsync(procurement.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ProcurementRequest?>(procurement));

        var result = await _service.GetByIdForUserAsync(procurement.Id, requester.Id);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Unauthorized.AccessDenied");
        await _userRepo.DidNotReceive()
            .GetByIdAsync(otherRequester.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddCommentAsync_RequesterAccessingAnotherRequesterRequest_ReturnsUnauthorizedAndDoesNotPersist()
    {
        var requester = CreateUser("Alice Requester", UserRole.Requester);
        var otherRequester = CreateUser("Bob Requester", UserRole.Requester);
        var procurement = CreateSubmittedRequest(otherRequester.Id);

        _userRepo.GetByIdAsync(requester.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<User?>(requester));
        _procurementRepo.GetByIdAsync(procurement.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ProcurementRequest?>(procurement));

        var result = await _service.AddCommentAsync(
            procurement.Id,
            requester.Id,
            new AddCommentRequest("Peeking at this should not be allowed"));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Unauthorized.AccessDenied");
        procurement.Comments.Should().BeEmpty();
        await _procurementRepo.DidNotReceive()
            .UpdateAsync(Arg.Any<ProcurementRequest>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive()
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetMetricsForUserAsync_Requester_PassesRequesterRoleToRepository()
    {
        var requester = CreateUser("Alice Requester", UserRole.Requester);
        var metrics = new DashboardMetrics(
            TotalRequests: 1,
            DraftCount: 1,
            PendingApprovalCount: 0,
            ApprovedCount: 0,
            RejectedCount: 0,
            PurchaseOrderCount: 0,
            TotalApprovedAmount: 0,
            AverageProcessingTimeHours: 0,
            StatusBreakdown: [new StatusCount("Draft", 1)],
            DepartmentBreakdown: [new DepartmentCount("Engineering", 1, 5000m)]);

        _userRepo.GetByIdAsync(requester.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<User?>(requester));
        _procurementRepo.GetMetricsAsync(requester.Id, requester.Role, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(metrics));

        var result = await _service.GetMetricsForUserAsync(requester.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalRequests.Should().Be(1);
        await _procurementRepo.Received(1)
            .GetMetricsAsync(requester.Id, UserRole.Requester, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApproveByManagerAsync_NonManager_ReturnsUnauthorizedAndDoesNotPersist()
    {
        var requester = CreateUser("Alice Requester", UserRole.Requester);
        var nonManager = CreateUser("Riley Requester", UserRole.Requester);
        var procurement = CreateSubmittedRequest(requester.Id);

        _procurementRepo.GetByIdAsync(procurement.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ProcurementRequest?>(procurement));
        _userRepo.GetByIdAsync(nonManager.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<User?>(nonManager));

        var result = await _service.ApproveByManagerAsync(
            procurement.Id,
            nonManager.Id,
            new ApprovalRequest("Looks good"));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Unauthorized.NotManager");
        procurement.Status.Should().Be(ProcurementStatus.Submitted);
        await _procurementRepo.DidNotReceive()
            .UpdateAsync(Arg.Any<ProcurementRequest>(), Arg.Any<CancellationToken>());
        await _notificationRepo.DidNotReceive()
            .AddAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive()
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApproveByManagerAsync_ManagerApproval_NotifiesRequesterAndFinance()
    {
        var requester = CreateUser("Alice Requester", UserRole.Requester);
        var manager = CreateUser("Manny Manager", UserRole.Manager);
        var finance = CreateUser("Frances Finance", UserRole.Finance);
        var procurement = CreateSubmittedRequest(requester.Id);

        _procurementRepo.GetByIdAsync(procurement.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ProcurementRequest?>(procurement));
        _userRepo.GetByIdAsync(manager.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<User?>(manager));
        _userRepo.GetByIdAsync(requester.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<User?>(requester));
        _userRepo.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<User>>(new List<User> { manager }));
        _userRepo.GetByRoleAsync(UserRole.Finance, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<User>>(new List<User> { finance }));

        var result = await _service.ApproveByManagerAsync(
            procurement.Id,
            manager.Id,
            new ApprovalRequest("Approved by manager"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("ManagerApproved");
        result.Value.AuditTrail.Should().ContainSingle(a =>
            a.Action == "Manager Approved" &&
            a.UserId == manager.Id &&
            a.UserName == manager.Name &&
            a.Comment == "Approved by manager");
        await _notificationRepo.Received(1)
            .AddAsync(Arg.Is<Notification>(n =>
                n.UserId == requester.Id &&
                n.Title == "Request Approved by Manager" &&
                n.ReferenceId == procurement.Id), Arg.Any<CancellationToken>());
        await _notificationRepo.Received(1)
            .AddAsync(Arg.Is<Notification>(n =>
                n.UserId == finance.Id &&
                n.Title == "Procurement Pending Finance Review" &&
                n.ReferenceId == procurement.Id), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1)
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static ProcurementRequest CreateDraftWithItem(Guid requesterId)
    {
        var procurement = ProcurementRequest.Create(
            "Laptop refresh",
            "Engineering laptop refresh",
            Department.Engineering,
            Urgency.High,
            requesterId);
        procurement.AddLineItem("MacBook Pro", 2, 2500m).IsSuccess.Should().BeTrue();
        return procurement;
    }

    private static ProcurementRequest CreateSubmittedRequest(Guid requesterId)
    {
        var procurement = CreateDraftWithItem(requesterId);
        procurement.Submit(requesterId).IsSuccess.Should().BeTrue();
        return procurement;
    }

    private static User CreateUser(string name, UserRole role) =>
        User.Create(name, $"{name.Replace(" ", ".").ToLowerInvariant()}@example.com", role, Department.Engineering);
}
