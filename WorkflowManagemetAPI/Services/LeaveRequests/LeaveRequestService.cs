using System.Security.Claims;
using WorkflowManagemetAPI.DTOs.LeaveRequests;
using WorkflowManagemetAPI.Entities;
using WorkflowManagemetAPI.Events;
using WorkflowManagemetAPI.Interfaces.LeaveRequests;
using WorkflowManagemetAPI.Interfaces.Messaging;
using WorkflowManagemetAPI.Interfaces.UnitOfWork;
using WorkflowManagemetAPI.UnitOfWork;
using Microsoft.AspNetCore.Http;

namespace WorkflowManagemetAPI.Services.LeaveRequests
{
    public class LeaveRequestService : ILeaveRequestService
    {
        private readonly IEmployeeUnitOfWork _employeeUow;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ApproverResolver _approverResolver;
        private readonly IEventPublisher _eventPublisher;

        public LeaveRequestService(
            IEmployeeUnitOfWork employeeUow,
            IHttpContextAccessor httpContextAccessor,
            ApproverResolver approverResolver,
            IEventPublisher eventPublisher)
        {
            _employeeUow = employeeUow;
            _httpContextAccessor = httpContextAccessor;
            _approverResolver = approverResolver;
            _eventPublisher = eventPublisher;
        }

        public List<LeaveRequestListItemDto> GetMine()
        {
            // Token → UserId (Create ile aynı mantık)
            var userIdClaim = _httpContextAccessor.HttpContext?.User?
                .FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(userIdClaim))
                throw new UnauthorizedAccessException("Kullanıcı doğrulanamadı.");

            var userId = int.Parse(userIdClaim);

            // Employee
            var employee = _employeeUow.Employees.GetByUserId(userId);
            if (employee == null)
                throw new Exception("Employee bulunamadı.");

            var items = _employeeUow.LeaveRequests.GetByEmployeeId(employee.EmployeeId);

            return items.Select(x => new LeaveRequestListItemDto
            {
                LeaveRequestId = x.LeaveRequestId,
                EmployeeId = x.EmployeeId,
                ApproverEmployeeId = x.ApproverEmployeeId,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                DayCount = x.DayCount,
                Reason = x.Reason,
                Status = x.Status,
                CreatedAtUtc = x.CreatedAtUtc
            }).ToList();
        }

        public LeaveRequestResponseDto Create(CreateLeaveRequestRequest request)
        {
            // 1) Token → UserId
            var userIdClaim = _httpContextAccessor.HttpContext?.User?
                .FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(userIdClaim))
                throw new UnauthorizedAccessException("Kullanıcı doğrulanamadı.");

            var userId = int.Parse(userIdClaim);

            // 2) Employee
            var employee = _employeeUow.Employees.GetByUserId(userId);
            if (employee == null)
                throw new Exception("Employee bulunamadı.");

            // 3) Gün sayısı
            var dayCount = (request.EndDate.Date - request.StartDate.Date).Days + 1;
            if (dayCount <= 0)
                throw new Exception("Tarih aralığı geçersiz.");

            // 4) Approver çöz
            var approverEmployeeId =
                _approverResolver.ResolveApproverEmployeeId(employee.Path, dayCount);

            if (!approverEmployeeId.HasValue)
                throw new Exception("Üst yönetici bulunamadı. En üst yönetici izin talebi oluşturamaz.");

            // 5) LeaveRequest oluştur
            var leaveRequest = new LeaveRequest
            {
                EmployeeId = employee.EmployeeId,
                ApproverEmployeeId = approverEmployeeId.Value,
                StartDate = request.StartDate.Date,
                EndDate = request.EndDate.Date,
                DayCount = dayCount,
                Reason = request.Reason,
                Status = "Pending",
                CreatedAtUtc = DateTime.UtcNow
            };

            _employeeUow.LeaveRequests.Add(leaveRequest);
            _employeeUow.SaveChanges();

            var evt = new LeaveRequestCreatedEvent
            {
                LeaveRequestId = leaveRequest.LeaveRequestId,
                EmployeeId = leaveRequest.EmployeeId,
                ApproverEmployeeId = leaveRequest.ApproverEmployeeId,
                StartDate = leaveRequest.StartDate,
                EndDate = leaveRequest.EndDate,
                DayCount = leaveRequest.DayCount,
                Reason = leaveRequest.Reason
            };

            _eventPublisher.Publish("LeaveRequestCreated", evt);

            // 6) Response
            return new LeaveRequestResponseDto
            {
                LeaveRequestId = leaveRequest.LeaveRequestId,
                Status = leaveRequest.Status,
                ApproverEmployeeId = leaveRequest.ApproverEmployeeId,
                DayCount = leaveRequest.DayCount
            };
        }

        public List<LeaveRequestListItemDto> GetMine(int employeeId)
        {
            var items = _employeeUow.LeaveRequests.GetByEmployeeId(employeeId);

            items ??= new List<LeaveRequest>();

            return items.Select(x => new LeaveRequestListItemDto
            {
                LeaveRequestId = x.LeaveRequestId,
                EmployeeId = x.EmployeeId,
                ApproverEmployeeId = x.ApproverEmployeeId,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                DayCount = x.DayCount,
                Reason = x.Reason,
                Status = x.Status,
                CreatedAtUtc = x.CreatedAtUtc
            }).ToList();
        }


    }
}
