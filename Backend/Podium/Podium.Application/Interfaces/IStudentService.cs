using Podium.Application.DTOs.Student;
using Podium.Application.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Application.Interfaces
{
    /// <summary>
    /// Service for student-related business logic with authorization checks
    /// </summary>
    public interface IStudentService
    {
        Task<ServiceResult<StudentDetailsDto>> GetStudentDetailsAsync(int studentId);
        Task<ServiceResult<bool>> UpdateStudentProfileAsync(int studentId, UpdateStudentDto dto);
        Task<ServiceResult<IEnumerable<StudentDetailsDto>>> GetAccessibleStudentsAsync();
    }
}
