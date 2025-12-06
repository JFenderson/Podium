using BandRecruitment.Services;
using Podium.Application.DTOs.Student;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Core.Interfaces
{
    public interface IStudentService
    {
        Task<ServiceResult<StudentDetailsDto>> GetStudentDetailsAsync(int studentId);
        Task<ServiceResult<bool>> UpdateStudentProfileAsync(int studentId, UpdateStudentDto dto);
        Task<ServiceResult<IEnumerable<StudentDetailsDto>>> GetAccessibleStudentsAsync();
    }
}
