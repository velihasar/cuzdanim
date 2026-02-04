using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace Core.Entities.Concrete
{
    public class User : IEntity
    {
        public User()
        {
            if(UserId==0){
              RecordDate = DateTime.Now;
            }
            UpdateContactDate = DateTime.Now;
            Status = true;
        }

        public int UserId { get; set; }
        //public long CitizenId { get; set; }
        public string UserName { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        [JsonIgnore]
        public string RefreshToken { get; set; }
        public string MobilePhones { get; set; }
        public bool Status { get; set; }
        public DateTime BirthDate { get; set; }
        public int Gender { get; set; }
        public DateTime RecordDate { get; set; }
        public string Address { get; set; }
        public string Notes { get; set; }
        public DateTime UpdateContactDate { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public string EmailVerificationToken { get; set; }
        public DateTime? EmailVerificationTokenExpiry { get; set; }

        // Email değişikliği için alanlar
        public string PendingEmail { get; set; } // Şifrelenmiş yeni email (doğrulanmayı bekliyor)
        public string EmailChangeToken { get; set; } // Email değişikliği için token
        public DateTime? EmailChangeTokenExpiry { get; set; } // Token süresi

        // Şifre sıfırlama için alanlar
        public string PasswordResetToken { get; set; } // Şifre sıfırlama için token (6 haneli kod)
        public DateTime? PasswordResetTokenExpiry { get; set; } // Token süresi

        // Google Sign-In için alanlar
        public string GoogleId { get; set; } // Google kullanıcı ID'si (sub claim)

        // Firebase Cloud Messaging için alanlar
        public string FcmToken { get; set; } // Firebase Cloud Messaging device token

        /// <summary>
        /// This is required when encoding token. Not in db. The default is Person.
        /// </summary>
        [NotMapped]
        public string AuthenticationProviderType { get; set; } = "Person";

        public byte[] PasswordSalt { get; set; }
        public byte[] PasswordHash { get; set; }

        // Navigation properties for wallet entities
        public virtual ICollection<Asset> Assets { get; set; } = new List<Asset>();
        public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
        public virtual ICollection<ExpenseCategory> ExpenseCategories { get; set; } = new List<ExpenseCategory>();
        public virtual ICollection<IncomeCategory> IncomeCategories { get; set; } = new List<IncomeCategory>();

        public bool UpdateMobilePhone(string mobilePhone)
        {
            if (mobilePhone == MobilePhones)
            {
                return false;
            }

            MobilePhones = mobilePhone;
            return true;
        }
    }
}
