using Core.Entities;
using Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Dtos.AssetType
{
    public class AssetTypeGetAllDto:IDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public AssetConvertType ConvertedAmountType { get; set; }
        public decimal TlValue { get; set; }
        public string ApiUrlKey { get; set; }
        public int? UserId { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }
}
