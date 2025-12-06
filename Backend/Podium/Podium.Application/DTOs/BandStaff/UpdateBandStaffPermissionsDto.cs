using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Application.DTOs.BandStaff
{
  public class UpdateBandStaffPermissionsDto
{
    public bool CanViewStudents { get; set; }
    public bool CanRateStudents { get; set; }
    public bool CanSendOffers { get; set; }
    public bool CanManageEvents { get; set; }
    public bool CanManageStaff { get; set; }
}
}
