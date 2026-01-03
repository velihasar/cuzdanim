using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DataAccess.Migrations.Pg
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GroupClaims",
                columns: table => new
                {
                    GroupId = table.Column<int>(type: "integer", nullable: false),
                    ClaimId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupClaims", x => new { x.GroupId, x.ClaimId });
                });

            migrationBuilder.CreateTable(
                name: "Groups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GroupName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<int>(type: "integer", nullable: true),
                    UpdatedBy = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Groups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Languages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<int>(type: "integer", nullable: true),
                    UpdatedBy = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Languages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Logs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MessageTemplate = table.Column<string>(type: "text", nullable: true),
                    Level = table.Column<string>(type: "text", nullable: true),
                    TimeStamp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Exception = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Logs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MobileLogins",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Provider = table.Column<int>(type: "integer", nullable: false),
                    ExternalUserId = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Code = table.Column<int>(type: "integer", maxLength: 50, nullable: false),
                    SendDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsSend = table.Column<bool>(type: "boolean", nullable: false),
                    IsUsed = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<int>(type: "integer", nullable: true),
                    UpdatedBy = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MobileLogins", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OperationClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Alias = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Description = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<int>(type: "integer", nullable: true),
                    UpdatedBy = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperationClaims", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<int>(type: "integer", nullable: true),
                    UpdatedBy = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Translates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LangId = table.Column<int>(type: "integer", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Value = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<int>(type: "integer", nullable: true),
                    UpdatedBy = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Translates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserClaims",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    ClaimId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserClaims", x => new { x.UserId, x.ClaimId });
                });

            migrationBuilder.CreateTable(
                name: "UserGroups",
                columns: table => new
                {
                    GroupId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserGroups", x => new { x.UserId, x.GroupId });
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FullName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RefreshToken = table.Column<string>(type: "text", nullable: true),
                    MobilePhones = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Status = table.Column<bool>(type: "boolean", nullable: false),
                    BirthDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Gender = table.Column<int>(type: "integer", nullable: false),
                    RecordDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Address = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    UpdateContactDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    LastLoginDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    EmailVerificationToken = table.Column<string>(type: "text", nullable: true),
                    EmailVerificationTokenExpiry = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    PasswordSalt = table.Column<byte[]>(type: "bytea", nullable: true),
                    PasswordHash = table.Column<byte[]>(type: "bytea", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "AssetTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: true),
                    ConvertedAmountType = table.Column<int>(type: "integer", nullable: false),
                    TlValue = table.Column<decimal>(type: "numeric", nullable: false),
                    ApiUrlKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<int>(type: "integer", nullable: true),
                    UpdatedBy = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssetTypes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ExpenseCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<int>(type: "integer", nullable: true),
                    UpdatedBy = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExpenseCategories_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "IncomeCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<int>(type: "integer", nullable: true),
                    UpdatedBy = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncomeCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IncomeCategories_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "Assets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    AssetTypeId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Piece = table.Column<int>(type: "integer", nullable: false),
                    IsDebt = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<int>(type: "integer", nullable: true),
                    UpdatedBy = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Assets_AssetTypes_AssetTypeId",
                        column: x => x.AssetTypeId,
                        principalTable: "AssetTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Assets_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Transactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    AssetId = table.Column<int>(type: "integer", nullable: true),
                    IncomeCategoryId = table.Column<int>(type: "integer", nullable: true),
                    ExpenseCategoryId = table.Column<int>(type: "integer", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    IsMonthlyRecurring = table.Column<bool>(type: "boolean", nullable: false),
                    IsBalanceCarriedOver = table.Column<bool>(type: "boolean", nullable: false),
                    DayOfMonth = table.Column<int>(type: "integer", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedBy = table.Column<int>(type: "integer", nullable: true),
                    UpdatedBy = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Transactions_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Transactions_ExpenseCategories_ExpenseCategoryId",
                        column: x => x.ExpenseCategoryId,
                        principalTable: "ExpenseCategories",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Transactions_IncomeCategories_IncomeCategoryId",
                        column: x => x.IncomeCategoryId,
                        principalTable: "IncomeCategories",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Transactions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Languages",
                columns: new[] { "Id", "Code", "CreatedBy", "CreatedDate", "IsActive", "Name", "UpdatedBy", "UpdatedDate" },
                values: new object[,]
                {
                    { 1, "tr-TR", null, null, true, "Türkçe", null, null },
                    { 2, "en-US", null, null, true, "English", null, null }
                });

            migrationBuilder.InsertData(
                table: "Translates",
                columns: new[] { "Id", "Code", "CreatedBy", "CreatedDate", "IsActive", "LangId", "UpdatedBy", "UpdatedDate", "Value" },
                values: new object[,]
                {
                    { 1, "Login", null, null, true, 1, null, null, "Giriş" },
                    { 2, "Email", null, null, true, 1, null, null, "E posta" },
                    { 3, "Password", null, null, true, 1, null, null, "Parola" },
                    { 4, "Update", null, null, true, 1, null, null, "Güncelle" },
                    { 5, "Delete", null, null, true, 1, null, null, "Sil" },
                    { 6, "UsersGroups", null, null, true, 1, null, null, "Kullanıcının Grupları" },
                    { 7, "UsersClaims", null, null, true, 1, null, null, "Kullanıcının Yetkileri" },
                    { 8, "Create", null, null, true, 1, null, null, "Yeni" },
                    { 9, "Users", null, null, true, 1, null, null, "Kullanıcılar" },
                    { 10, "Groups", null, null, true, 1, null, null, "Gruplar" },
                    { 11, "Login", null, null, true, 2, null, null, "Login" },
                    { 12, "Email", null, null, true, 2, null, null, "Email" },
                    { 13, "Password", null, null, true, 2, null, null, "Password" },
                    { 14, "Update", null, null, true, 2, null, null, "Update" },
                    { 15, "Delete", null, null, true, 2, null, null, "Delete" },
                    { 16, "UsersGroups", null, null, true, 2, null, null, "User's Groups" },
                    { 17, "UsersClaims", null, null, true, 2, null, null, "User's Claims" },
                    { 18, "Create", null, null, true, 2, null, null, "Create" },
                    { 19, "Users", null, null, true, 2, null, null, "Users" },
                    { 20, "Groups", null, null, true, 2, null, null, "Groups" },
                    { 21, "OperationClaim", null, null, true, 1, null, null, "Operasyon Yetkileri" },
                    { 22, "OperationClaim", null, null, true, 2, null, null, "Operation Claim" },
                    { 23, "Languages", null, null, true, 1, null, null, "Diller" },
                    { 24, "Languages", null, null, true, 2, null, null, "Languages" },
                    { 25, "TranslateWords", null, null, true, 1, null, null, "Dil Çevirileri" },
                    { 26, "TranslateWords", null, null, true, 2, null, null, "Translate Words" },
                    { 27, "Management", null, null, true, 1, null, null, "Yönetim" },
                    { 28, "Management", null, null, true, 2, null, null, "Management" },
                    { 29, "AppMenu", null, null, true, 1, null, null, "Uygulama" },
                    { 30, "AppMenu", null, null, true, 2, null, null, "Application" },
                    { 31, "Added", null, null, true, 1, null, null, "Başarıyla Eklendi." },
                    { 32, "Added", null, null, true, 2, null, null, "Successfully Added." },
                    { 33, "Updated", null, null, true, 1, null, null, "Başarıyla Güncellendi." },
                    { 34, "Updated", null, null, true, 2, null, null, "Successfully Updated." },
                    { 35, "Deleted", null, null, true, 1, null, null, "Başarıyla Silindi." },
                    { 36, "Deleted", null, null, true, 2, null, null, "Successfully Deleted." },
                    { 37, "OperationClaimExists", null, null, true, 1, null, null, "Bu operasyon izni zaten mevcut." },
                    { 38, "OperationClaimExists", null, null, true, 2, null, null, "This operation permit already exists." },
                    { 39, "StringLengthMustBeGreaterThanThree", null, null, true, 1, null, null, "Lütfen En Az 3 Karakterden Oluşan Bir İfade Girin." },
                    { 40, "StringLengthMustBeGreaterThanThree", null, null, true, 2, null, null, "Please Enter A Phrase Of At Least 3 Characters." },
                    { 41, "CouldNotBeVerifyCid", null, null, true, 1, null, null, "Kimlik No Doğrulanamadı." },
                    { 42, "CouldNotBeVerifyCid", null, null, true, 2, null, null, "Could not be verify Citizen Id" },
                    { 43, "VerifyCid", null, null, true, 1, null, null, "Kimlik No Doğrulandı." },
                    { 44, "VerifyCid", null, null, true, 2, null, null, "Verify Citizen Id" },
                    { 45, "AuthorizationsDenied", null, null, true, 1, null, null, "Yetkiniz olmayan bir alana girmeye çalıştığınız tespit edildi." },
                    { 46, "AuthorizationsDenied", null, null, true, 2, null, null, "It has been detected that you are trying to enter an area that you do not have authorization." },
                    { 47, "UserNotFound", null, null, true, 1, null, null, "Kimlik Bilgileri Doğrulanamadı. Lütfen Yeni Kayıt Ekranını kullanın." },
                    { 48, "UserNotFound", null, null, true, 2, null, null, "Credentials Could Not Verify. Please use the New Registration Screen." },
                    { 49, "PasswordError", null, null, true, 1, null, null, "Kimlik Bilgileri Doğrulanamadı, Kullanıcı adı ve/veya parola hatalı." },
                    { 50, "PasswordError", null, null, true, 2, null, null, "Credentials Failed to Authenticate, Username and / or password incorrect." },
                    { 51, "SuccessfulLogin", null, null, true, 1, null, null, "Sisteme giriş başarılı." },
                    { 52, "SuccessfulLogin", null, null, true, 2, null, null, "Login to the system is successful." },
                    { 53, "SendMobileCode", null, null, true, 1, null, null, "Lütfen Size SMS Olarak Gönderilen Kodu Girin!" },
                    { 54, "SendMobileCode", null, null, true, 2, null, null, "Please Enter The Code Sent To You By SMS!" },
                    { 55, "NameAlreadyExist", null, null, true, 1, null, null, "Oluşturmaya Çalıştığınız Nesne Zaten Var." },
                    { 56, "NameAlreadyExist", null, null, true, 2, null, null, "The Object You Are Trying To Create Already Exists." },
                    { 57, "WrongCID", null, null, true, 1, null, null, "Vatandaşlık No Sistemimizde Bulunamadı. Lütfen Yeni Kayıt Oluşturun!" },
                    { 58, "WrongCID", null, null, true, 2, null, null, "Citizenship Number Not Found In Our System. Please Create New Registration!" },
                    { 59, "CID", null, null, true, 1, null, null, "Vatandaşlık No" },
                    { 60, "CID", null, null, true, 2, null, null, "Citizenship Number" },
                    { 61, "PasswordEmpty", null, null, true, 1, null, null, "Parola boş olamaz!" },
                    { 62, "PasswordEmpty", null, null, true, 2, null, null, "Password can not be empty!" },
                    { 63, "PasswordLength", null, null, true, 1, null, null, "Minimum 8 Karakter Uzunluğunda Olmalıdır!" },
                    { 64, "PasswordLength", null, null, true, 2, null, null, "Must be at least 8 characters long! " },
                    { 65, "PasswordUppercaseLetter", null, null, true, 1, null, null, "En Az 1 Büyük Harf İçermelidir!" },
                    { 66, "PasswordUppercaseLetter", null, null, true, 2, null, null, "Must Contain At Least 1 Capital Letter!" },
                    { 67, "PasswordLowercaseLetter", null, null, true, 1, null, null, "En Az 1 Küçük Harf İçermelidir!" },
                    { 68, "PasswordLowercaseLetter", null, null, true, 2, null, null, "Must Contain At Least 1 Lowercase Letter!" },
                    { 69, "PasswordDigit", null, null, true, 1, null, null, "En Az 1 Rakam İçermelidir!" },
                    { 70, "PasswordDigit", null, null, true, 2, null, null, "It Must Contain At Least 1 Digit!" },
                    { 71, "PasswordSpecialCharacter", null, null, true, 1, null, null, "En Az 1 Simge İçermelidir!" },
                    { 72, "PasswordSpecialCharacter", null, null, true, 2, null, null, "Must Contain At Least 1 Symbol!" },
                    { 73, "SendPassword", null, null, true, 1, null, null, "Yeni Parolanız E-Posta Adresinize Gönderildi." },
                    { 74, "SendPassword", null, null, true, 2, null, null, "Your new password has been sent to your e-mail address." },
                    { 75, "InvalidCode", null, null, true, 1, null, null, "Geçersiz Bir Kod Girdiniz!" },
                    { 76, "InvalidCode", null, null, true, 2, null, null, "You Entered An Invalid Code!" },
                    { 77, "SmsServiceNotFound", null, null, true, 1, null, null, "SMS Servisine Ulaşılamıyor." },
                    { 78, "SmsServiceNotFound", null, null, true, 2, null, null, "Unable to Reach SMS Service." },
                    { 79, "TrueButCellPhone", null, null, true, 1, null, null, "Bilgiler doğru. Cep telefonu gerekiyor." },
                    { 80, "TrueButCellPhone", null, null, true, 2, null, null, "The information is correct. Cell phone is required." },
                    { 81, "TokenProviderException", null, null, true, 1, null, null, "Token Provider boş olamaz!" },
                    { 82, "TokenProviderException", null, null, true, 2, null, null, "Token Provider cannot be empty!" },
                    { 83, "Unknown", null, null, true, 1, null, null, "Bilinmiyor!" },
                    { 84, "Unknown", null, null, true, 2, null, null, "Unknown!" },
                    { 85, "NewPassword", null, null, true, 1, null, null, "Yeni Parola:" },
                    { 86, "NewPassword", null, null, true, 2, null, null, "New Password:" },
                    { 87, "ChangePassword", null, null, true, 1, null, null, "Parola Değiştir" },
                    { 88, "ChangePassword", null, null, true, 2, null, null, "Change Password" },
                    { 89, "Save", null, null, true, 1, null, null, "Kaydet" },
                    { 90, "Save", null, null, true, 2, null, null, "Save" },
                    { 91, "GroupName", null, null, true, 1, null, null, "Grup Adı" },
                    { 92, "GroupName", null, null, true, 2, null, null, "Group Name" },
                    { 93, "FullName", null, null, true, 1, null, null, "Tam Adı" },
                    { 94, "FullName", null, null, true, 2, null, null, "Full Name" },
                    { 95, "Address", null, null, true, 1, null, null, "Adres" },
                    { 96, "Address", null, null, true, 2, null, null, "Address" },
                    { 97, "Notes", null, null, true, 1, null, null, "Notlar" },
                    { 98, "Notes", null, null, true, 2, null, null, "Notes" },
                    { 99, "ConfirmPassword", null, null, true, 1, null, null, "Parolayı Doğrula" },
                    { 100, "ConfirmPassword", null, null, true, 2, null, null, "Confirm Password" },
                    { 101, "Code", null, null, true, 1, null, null, "Kod" },
                    { 102, "Code", null, null, true, 2, null, null, "Code" },
                    { 103, "Alias", null, null, true, 1, null, null, "Görünen Ad" },
                    { 104, "Alias", null, null, true, 2, null, null, "Alias" },
                    { 105, "Description", null, null, true, 1, null, null, "Açıklama" },
                    { 106, "Description", null, null, true, 2, null, null, "Description" },
                    { 107, "Value", null, null, true, 1, null, null, "Değer" },
                    { 108, "Value", null, null, true, 2, null, null, "Value" },
                    { 109, "LangCode", null, null, true, 1, null, null, "Dil Kodu" },
                    { 110, "LangCode", null, null, true, 2, null, null, "Lang Code" },
                    { 111, "Name", null, null, true, 1, null, null, "Adı" },
                    { 112, "Name", null, null, true, 2, null, null, "Name" },
                    { 113, "MobilePhones", null, null, true, 1, null, null, "Cep Telefonu" },
                    { 114, "MobilePhones", null, null, true, 2, null, null, "Mobile Phone" },
                    { 115, "NoRecordsFound", null, null, true, 1, null, null, "Kayıt Bulunamadı" },
                    { 116, "NoRecordsFound", null, null, true, 2, null, null, "No Records Found" },
                    { 117, "Required", null, null, true, 1, null, null, "Bu alan zorunludur!" },
                    { 118, "Required", null, null, true, 2, null, null, "This field is required!" },
                    { 119, "Permissions", null, null, true, 1, null, null, "Permissions" },
                    { 120, "Permissions", null, null, true, 2, null, null, "İzinler" },
                    { 121, "GroupList", null, null, true, 1, null, null, "Grup Listesi" },
                    { 122, "GroupList", null, null, true, 2, null, null, "Group List" },
                    { 123, "GrupPermissions", null, null, true, 1, null, null, "Grup Yetkileri" },
                    { 124, "GrupPermissions", null, null, true, 2, null, null, "Grup Permissions" },
                    { 125, "Add", null, null, true, 1, null, null, "Ekle" },
                    { 126, "Add", null, null, true, 2, null, null, "Add" },
                    { 127, "UserList", null, null, true, 1, null, null, "Kullanıcı Listesi" },
                    { 128, "UserList", null, null, true, 2, null, null, "User List" },
                    { 129, "OperationClaimList", null, null, true, 1, null, null, "Yetki Listesi" },
                    { 130, "OperationClaimList", null, null, true, 2, null, null, "OperationClaim List" },
                    { 131, "LanguageList", null, null, true, 1, null, null, "Dil Listesi" },
                    { 132, "LanguageList", null, null, true, 2, null, null, "Language List" },
                    { 133, "TranslateList", null, null, true, 1, null, null, "Dil Çeviri Listesi" },
                    { 134, "TranslateList", null, null, true, 2, null, null, "Translate List" },
                    { 135, "LogList", null, null, true, 1, null, null, "İşlem Kütüğü" },
                    { 136, "LogList", null, null, true, 2, null, null, "LogList" },
                    { 137, "DeleteConfirm", null, null, true, 1, null, null, "Emin misiniz?" },
                    { 138, "DeleteConfirm", null, null, true, 2, null, null, "Are you sure?" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Assets_AssetTypeId",
                table: "Assets",
                column: "AssetTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_UserId",
                table: "Assets",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetTypes_UserId",
                table: "AssetTypes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseCategories_UserId",
                table: "ExpenseCategories",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_IncomeCategories_UserId",
                table: "IncomeCategories",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MobileLogins_ExternalUserId_Provider",
                table: "MobileLogins",
                columns: new[] { "ExternalUserId", "Provider" });

            migrationBuilder.CreateIndex(
                name: "IX_SystemSettings_Key",
                table: "SystemSettings",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_AssetId",
                table: "Transactions",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_ExpenseCategoryId",
                table: "Transactions",
                column: "ExpenseCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_IncomeCategoryId",
                table: "Transactions",
                column: "IncomeCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_UserId",
                table: "Transactions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_Users_MobilePhones",
                table: "Users",
                column: "MobilePhones");

            migrationBuilder.CreateIndex(
                name: "IX_Users_UserId",
                table: "Users",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_UserName",
                table: "Users",
                column: "UserName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GroupClaims");

            migrationBuilder.DropTable(
                name: "Groups");

            migrationBuilder.DropTable(
                name: "Languages");

            migrationBuilder.DropTable(
                name: "Logs");

            migrationBuilder.DropTable(
                name: "MobileLogins");

            migrationBuilder.DropTable(
                name: "OperationClaims");

            migrationBuilder.DropTable(
                name: "SystemSettings");

            migrationBuilder.DropTable(
                name: "Transactions");

            migrationBuilder.DropTable(
                name: "Translates");

            migrationBuilder.DropTable(
                name: "UserClaims");

            migrationBuilder.DropTable(
                name: "UserGroups");

            migrationBuilder.DropTable(
                name: "Assets");

            migrationBuilder.DropTable(
                name: "ExpenseCategories");

            migrationBuilder.DropTable(
                name: "IncomeCategories");

            migrationBuilder.DropTable(
                name: "AssetTypes");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
