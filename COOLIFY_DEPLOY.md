# 🚀 Coolify Deploy Rehberi - Cüzdanım Backend

## 📋 Proje Özeti
- **Framework**: .NET 9.0 Web API
- **Database**: PostgreSQL (Ana), SQL Server, Oracle, MySQL desteği
- **Cache**: Redis
- **Background Jobs**: Hangfire
- **Authentication**: JWT Bearer Token
- **Google OAuth**: Destekleniyor

## 🔧 Coolify Deploy Adımları

### 1. Repository Hazırlığı
```bash
# Projeyi Git repository'sine push edin
git add .
git commit -m "Coolify deploy için hazırlık"
git push origin main
```

### 2. Coolify'da Yeni Uygulama Oluşturma

1. **Coolify Dashboard**'a giriş yapın
2. **"New Application"** butonuna tıklayın
3. **"Docker Compose"** seçeneğini seçin
4. Repository URL'nizi girin: `https://github.com/yourusername/Cuzdanim_Backend`
5. **Branch**: `main` veya `Production`

### 3. Environment Variables Ayarlama

Coolify'da **Environment Variables** sekmesinde aşağıdaki değişkenleri ekleyin:

#### Database Configuration
```
DB_HOST=postgres
DB_PORT=5432
DB_NAME=CuzdanimDb
DB_USER=postgres
DB_PASSWORD=Adana.14531989
```

#### JWT Configuration
```
JWT_AUDIENCE=cuzdanim.masavtech.com
JWT_ISSUER=cuzdanim.masavtech.com
JWT_SECURITY_KEY=CuzdanimMasavTech2024!SecureJWTKey48Chars!!
```

#### Turnstile Configuration (Opsiyonel)
```
TURNSTILE_SITE_KEY=
TURNSTILE_SECRET_KEY=
```

#### Email Configuration
```
SMTP_SERVER=smtp.gmail.com
SMTP_PORT=587
SMTP_SENDER_NAME=Cuzdanim
SMTP_SENDER_EMAIL=noreply@cuzdanim.com
SMTP_USERNAME=your_email@gmail.com
SMTP_PASSWORD=your_email_password
```

#### Application URLs
```
BASE_URL=https://api.cuzdanim.com
FRONTEND_URL=https://cuzdanim.com
```

#### Redis Configuration
```
REDIS_HOST=redis
REDIS_PORT=6379
REDIS_PASSWORD=your_redis_password
```

#### RabbitMQ Configuration
```
RABBITMQ_HOST=rabbitmq
RABBITMQ_USERNAME=guest
RABBITMQ_PASSWORD=guest
```

#### Hangfire Configuration
```
HANGFIRE_DB_NAME=cuzdanim_hangfire
HANGFIRE_CONNECTION_STRING=Host=${DB_HOST};Port=${DB_PORT};Database=${HANGFIRE_DB_NAME};Username=${DB_USER};Password=${DB_PASSWORD}
HANGFIRE_USERNAME=hangfire_velihasar
HANGFIRE_PASSWORD=Adana.14531989
```

#### MongoDB Configuration
```
MONGODB_CONNECTIONSTRING=mongodb://mongodb:27017
MONGODB_DATABASE=cuzdanim_logs
```

#### Elasticsearch Configuration
```
ELASTICSEARCH_URL=http://elasticsearch:9200
ELASTICSEARCH_USERNAME=
ELASTICSEARCH_PASSWORD=
```

#### Teams Webhook (Opsiyonel)
```
TEAMS_WEBHOOK_URL=
```

#### Email Encryption
```
EMAIL_ENCRYPTION_KEY=CuzdanimMasavTech2024!Key32!!
```

#### Google OAuth
```
GOOGLE_CLIENT_ID=137422679898-3pu4dpsf86cc8jsoa07jdfml84nr016v.apps.googleusercontent.com
```

**Not:** Bu Web Application Client ID'dir. Backend Google ID token doğrulaması için kullanır. Android keystore bilgileri backend'de gerekli değildir (sadece Google Console'da Android Client ID'ye SHA-1 eklenir).

#### Admin Settings
```
ADMIN_EMAIL=admin@cuzdanim.com
ADMIN_USERNAME=velihasar
ADMIN_PASSWORD=Adana.14531989
ADMIN_FULL_NAME=System Administrator
```

### 4. Database Migration

Deploy işleminden önce PostgreSQL veritabanını hazırlamanız gerekiyor:

1. **Coolify'da PostgreSQL servisi** oluşturun
2. **Database migration** otomatik olarak uygulanacak (Startup.cs'de `db.Database.Migrate()` mevcut)
3. Manuel migration gerekirse:
```bash
# Coolify terminal'de
dotnet ef database update --project DataAccess --startup-project WebAPI
```

### 5. Domain ve SSL Ayarları

1. **Domain** sekmesinde domain adınızı ekleyin
2. **SSL Certificate** otomatik olarak Let's Encrypt ile oluşturulacak
3. **Force HTTPS** seçeneğini aktif edin

### 6. Monitoring ve Logs

- **Logs** sekmesinden uygulama loglarını takip edebilirsiniz
- **Health Check** endpoint'i: `/health` (varsa)
- **Hangfire Dashboard**: `/hangfire` (hangfire_velihasar/Adana.14531989 ile giriş)
- **Swagger UI**: Production'da kapalı (sadece Development/Staging'de açık)

## 🔍 Troubleshooting

### Yaygın Sorunlar ve Çözümleri

#### 1. Database Connection Hatası
```bash
# PostgreSQL servisinin çalıştığını kontrol edin
docker ps | grep postgres

# Connection string'i kontrol edin
echo $DB_HOST
echo $DB_NAME
echo $DB_USER
```

**Çözüm:**
- PostgreSQL servisinin çalıştığından emin olun
- Environment variable'ların doğru set edildiğini kontrol edin
- Database'in oluşturulduğunu kontrol edin

#### 2. Migration Hatası
```bash
# Migration durumunu kontrol edin
dotnet ef migrations list --project DataAccess --startup-project WebAPI
```

**Çözüm:**
- Migration'ların otomatik uygulandığını kontrol edin (Startup.cs'de `db.Database.Migrate()`)
- Manuel migration gerekirse yukarıdaki komutu kullanın

#### 3. Hangfire Connection Hatası
```bash
# Hangfire database'inin oluşturulduğunu kontrol edin
psql -h $DB_HOST -U $DB_USER -d $HANGFIRE_DB_NAME -c "SELECT 1;"
```

**Çözüm:**
- `HANGFIRE_DB_NAME` environment variable'ının set edildiğini kontrol edin
- Hangfire database'inin oluşturulduğundan emin olun

#### 4. Redis Connection Hatası
```bash
# Redis servisinin çalıştığını kontrol edin
docker ps | grep redis

# Redis CLI ile test
redis-cli -h redis -p 6379 -a your_password ping
```

#### 5. JWT Token Hatası
- JWT_SECURITY_KEY'in yeterince uzun ve güvenli olduğundan emin olun
- JWT_AUDIENCE ve JWT_ISSUER'ın doğru set edildiğini kontrol edin

#### 6. Google OAuth Hatası
- GOOGLE_CLIENT_ID'in doğru set edildiğini kontrol edin
- Google Cloud Console'da redirect URI'ların doğru yapılandırıldığını kontrol edin

#### 7. GitHub Repository Authentication Hatası
**Hata Mesajı:**
```
fatal: could not read Username for 'https://github.com': No such device or address
```

**Neden:**
- Repository private ise ve Coolify'da GitHub credentials yapılandırılmamışsa
- HTTPS URL kullanılıyorsa ve authentication gerekiyorsa
- Coolify'ın GitHub'a erişim izni yoksa

**Çözümler:**

**Çözüm 1: GitHub OAuth App (En Önerilen - Coolify'da OAuth Altında)**
1. **GitHub'da OAuth App Oluşturma:**
   - GitHub'da **Settings** → **Developer settings** → **OAuth Apps** → **New OAuth App**
   - **Application name:** `Coolify - Cuzdanim` (veya istediğiniz bir isim)
   - **Homepage URL:** `https://your-coolify-domain.com` (Coolify domain'iniz)
   - **Authorization callback URL:** 
     - Coolify Dashboard'da **Settings** → **OAuth** → **GitHub** bölümüne gidin
     - Burada **Redirect URL** veya **Callback URL** gösterilir
     - ⚠️ **Önemli:** GitHub OAuth callback URL'leri **port numarası kabul etmez** ve **HTTPS gerektirir**
     - Eğer Coolify'ın gösterdiği URL'de port varsa (örn: `:8080`), port'u kaldırın
     - Örnek formatlar:
       - ✅ **Doğru:** `https://ik0cwwkokgcowogssg448g0o.217.195.207.219.sslip.io/oauth/github/callback`
       - ❌ **Yanlış:** `http://ik0cwwkokgcowogssg448g0o.217.195.207.219.sslip.io:8080/oauth/github/callback`
     - **Not:** Eğer sslip.io domain'i HTTPS desteklemiyorsa, Coolify'da özel domain kullanmanız gerekebilir
   - **Register application** butonuna tıklayın

2. **Client ID ve Client Secret Alma:**
   - OAuth App oluşturulduktan sonra **Client ID** ve **Client Secret** göreceksiniz
   - **Client Secret**'ı hemen kopyalayın (bir daha gösterilmeyebilir)
   - Gerekirse **Generate a new client secret** ile yeni secret oluşturabilirsiniz

3. **Coolify'da OAuth Yapılandırması:**
   - Coolify Dashboard'da **Settings** → **OAuth** → **GitHub** bölümüne gidin
   - **Client ID:** GitHub'dan aldığınız Client ID'yi yapıştırın
   - **Client Secret:** GitHub'dan aldığınız Client Secret'ı yapıştırın
   - **Redirect URL:** Coolify otomatik olarak gösterir, bu URL'yi GitHub OAuth App'te kullanın
   - **Save** butonuna tıklayın

4. **Repository Erişimi:**
   - Coolify'da **Settings** → **Source Providers** → **GitHub** bölümüne gidin
   - OAuth ile bağlanın (artık Client ID ve Secret ile authentication yapılacak)
   - Repository URL'yi tekrar deneyin: `https://github.com/velihasar/cuzdanim`

**Not:** OAuth App kullanarak daha güvenli ve yönetilebilir bir authentication sağlarsınız. Token'lar otomatik olarak yenilenir.

**Çözüm 2: GitHub Personal Access Token (Alternatif)**
1. GitHub'da **Settings** → **Developer settings** → **Personal access tokens** → **Tokens (classic)**
2. **Generate new token (classic)** ile yeni token oluşturun
3. Gerekli izinleri seçin: `repo` (private repository için)
4. Token'ı kopyalayın
5. Coolify Dashboard'da:
   - **Settings** → **Source Providers** → **GitHub**
   - Token'ı ekleyin veya güncelleyin
   - Repository URL'yi tekrar deneyin

**Çözüm 2: SSH URL Kullanma**
1. Coolify'da repository URL'yi değiştirin:
   - **HTTPS:** `https://github.com/velihasar/cuzdanim`
   - **SSH:** `git@github.com:velihasar/cuzdanim.git`
2. Coolify'da SSH key yapılandırın:
   - **Settings** → **Source Providers** → **GitHub**
   - SSH key ekleyin (GitHub'da SSH key oluşturup Coolify'a ekleyin)

**Çözüm 3: Repository'yi Public Yapma (Güvenlik Riski)**
- Repository'yi public yaparsanız authentication gerekmez
- ⚠️ **Önerilmez:** Kodunuz herkese açık olur

**Çözüm 4: Deploy Key Kullanma**
1. GitHub repository'de **Settings** → **Deploy keys** → **Add deploy key**
2. Coolify'ın SSH public key'ini ekleyin
3. **Allow write access** seçeneğini işaretleyin (gerekirse)
4. Repository URL'yi SSH formatında kullanın: `git@github.com:velihasar/cuzdanim.git`

**En İyi Pratik:**
- Private repository için **Personal Access Token** kullanın
- Token'ı düzenli olarak yenileyin
- Token'ı sadece gerekli izinlerle oluşturun (`repo` scope yeterli)

## 📊 Performans Optimizasyonları

### 1. Database Optimizasyonu
- Connection pooling ayarlarını optimize edin
- Index'leri kontrol edin
- Query performance'ı izleyin
- Migration'lar otomatik uygulanıyor, manuel kontrol gerekmez

### 2. Cache Stratejisi
- Redis cache'i aktif kullanın
- Memory cache'i optimize edin
- Cache expiration policy'lerini ayarlayın

### 3. Background Jobs
- Hangfire dashboard'dan job'ları izleyin
- Recurring job'ların düzgün çalıştığını kontrol edin
- Asset type price update job'unun çalıştığını kontrol edin

## 🔒 Güvenlik Önerileri

1. **Environment Variables**: Hassas bilgileri environment variable olarak saklayın ✅
2. **JWT Security**: Güçlü security key kullanın ✅
3. **Database**: Güçlü şifreler kullanın ✅
4. **HTTPS**: Her zaman HTTPS kullanın ✅
5. **CORS**: Sadece gerekli origin'leri allow edin
6. **Rate Limiting**: API rate limiting uygulayın
7. **Admin Credentials**: Production'da admin şifresini değiştirin
8. **Swagger**: Production'da Swagger'ı kapalı tutun ✅

## 📱 API Endpoints

### Authentication
- `POST /api/auth/login` - Kullanıcı girişi
- `POST /api/auth/register` - Kullanıcı kaydı
- `POST /api/auth/refresh` - Token yenileme
- `POST /api/auth/google-login` - Google OAuth girişi

### Assets
- `GET /api/assets` - Varlık listesi
- `POST /api/assets` - Yeni varlık ekleme
- `PUT /api/assets/{id}` - Varlık güncelleme
- `DELETE /api/assets/{id}` - Varlık silme

### Transactions
- `GET /api/transactions` - İşlem listesi
- `POST /api/transactions` - Yeni işlem ekleme
- `PUT /api/transactions/{id}` - İşlem güncelleme
- `DELETE /api/transactions/{id}` - İşlem silme

### Categories
- `GET /api/income-categories` - Gelir kategorileri
- `GET /api/expense-categories` - Gider kategorileri
- `GET /api/asset-types` - Varlık türleri

### Hangfire Dashboard
- `/hangfire` - Background job yönetimi (hangfire_velihasar/Adana.14531989)

## 🆘 Destek

Sorun yaşarsanız:
1. Coolify logs'unu kontrol edin
2. Docker container'larının durumunu kontrol edin
3. Environment variables'ları doğrulayın
4. Database connection'ını test edin
5. Migration'ların uygulandığını kontrol edin

## 📝 Önemli Notlar

1. **Otomatik Migration**: Uygulama başladığında otomatik olarak migration'lar uygulanır (Startup.cs'de `db.Database.Migrate()`)
2. **Admin Kullanıcı**: İlk kurulumda otomatik olarak admin kullanıcı oluşturulur (velihasar / Adana.14531989)
3. **Hangfire**: Background job'lar için Hangfire kullanılıyor, dashboard `/hangfire` adresinde
4. **Swagger**: Production'da Swagger kapalı, sadece Development/Staging'de açık
5. **Environment Variables**: Tüm hassas bilgiler environment variable olarak saklanıyor

---

**Not**: Bu rehber production ortamı için hazırlanmıştır. Development ortamında farklı ayarlar gerekebilir.

