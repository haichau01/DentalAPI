using DentalAPI.Domain.Authorization;

namespace DentalAPI.Application.Authorization;

/// <summary>Descriptions of each permission for the admin screen.</summary>
public static class PermissionCatalog
{
    private static readonly Dictionary<Permission, string> _descriptions = new()
    {
        [Permission.PatientsRead] = "Xem danh sách và hồ sơ bệnh nhân",
        [Permission.PatientsWrite] = "Tạo và sửa thông tin bệnh nhân",
        [Permission.PatientsDelete] = "Xóa bệnh nhân (xóa mềm)",
        [Permission.AppointmentsRead] = "Xem lịch hẹn",
        [Permission.AppointmentsWrite] = "Đặt, đổi và hủy lịch hẹn",
        [Permission.TreatmentRecordsRead] = "Xem hồ sơ điều trị",
        [Permission.TreatmentRecordsWrite] = "Ghi và sửa hồ sơ điều trị",
        [Permission.InvoicesRead] = "Xem hóa đơn và thanh toán",
        [Permission.InvoicesWrite] = "Tạo hóa đơn và ghi nhận thanh toán",
        [Permission.ServicesManage] = "Quản lý danh mục dịch vụ và đơn giá",
    };

    public static IReadOnlyList<PermissionDto> All { get; } = PermissionCodes.All
        .Select(permission => new PermissionDto(PermissionCodes.ToCode(permission), _descriptions[permission]))
        .ToList();
}
