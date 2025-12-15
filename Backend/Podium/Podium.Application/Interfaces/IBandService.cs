using Podium.Application.DTOs.Band;
using Podium.Application.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Application.Interfaces
{
    public interface IBandService
    {
        Task<ServiceResult<IEnumerable<BandSummaryDto>>> GetActiveBandsAsync(BandFilterDto filter);
        Task<ServiceResult<BandDetailDto>> GetBandDetailsAsync(int bandId);
    }
}
