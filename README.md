# TerziTuran

TerziTuran, terziler ve konfeksiyon atolyeleri icin gelistirilmis ASP.NET Core MVC + Web API + SQLite tabanli yonetim uygulamasidir. Bu surumde sadece web/backend tarafi olusturulmustur.

## Kullanilan Teknolojiler

- ASP.NET Core MVC
- ASP.NET Core Web API
- Entity Framework Core
- SQLite
- Cookie Authentication
- JWT Authentication
- Bootstrap 5
- Font Awesome
- Yerel animasyonlu Canvas grafik motoru
- QuestPDF
- Swagger

## Ozellikler

- Admin ve Staff rolleri
- Guvenli sifre hashleme
- Musteri, olcu, siparis, siparis kalemi, odeme ve randevu CRUD islemleri
- Yetki kontrollu kullanici yonetimi
- Dashboard istatistikleri ve grafikler
- Filtrelenebilir rapor ekrani
- PDF rapor cikisi
- Mobil uygulama entegrasyonu icin JWT korumali REST API

## Klasor Yapisi

```text
TerziTuran/
├── TerziTuran.Web/
│   ├── ApiControllers/
│   ├── Controllers/
│   ├── Data/
│   ├── DTOs/
│   ├── Middleware/
│   ├── Models/
│   ├── Services/
│   ├── ViewModels/
│   ├── Views/
│   └── wwwroot/
├── deployment/
├── README.md
└── .gitignore
```

## Kurulum

1. Proje klasorune girin:

```bash
cd TerziTuran/TerziTuran.Web
```

2. Paketleri yukleyin:

```bash
dotnet restore
```

3. Migration olusturun veya mevcut migration'i kullanin:

```bash
DOTNET_ROLL_FORWARD=Major dotnet ef migrations add InitialCreate
```

4. Veritabanini guncelleyin:

```bash
DOTNET_ROLL_FORWARD=Major dotnet ef database update
```

5. Uygulamayi baslatin:

```bash
DOTNET_ROLL_FORWARD=Major dotnet run
```

Not:
Bu makinede yalnizca .NET 10 runtime bulundugu icin proje `net8.0` hedefinde olsa bile calistirma ve `dotnet-ef` komutlarinda `DOTNET_ROLL_FORWARD=Major` kullanilmistir.

## Guvenli Giris ve Kayit

- Musteriler `/Auth/Register` ekranindan hesap olusturabilir ve dogrudan kendi portallarina girer.
- Acik kayit ucu yalnizca `Customer` rolu olusturur.
- Giris ve kayit istekleri IP bazli rate-limit ile korunur.
- Uretimde ornek hesap veya veri eklenmez.
- Ilk uretim yoneticisi, asagidaki ortam degiskenleriyle guvenli olarak olusturulur.

## Swagger

- Varsayilan gelistirme adresi: `https://localhost:xxxx/swagger`
- Testte kullanilan ornek adres: `http://127.0.0.1:5055/swagger`

## API Uclari

- `POST /api/auth/login`
- `POST /api/auth/register`
- `GET /api/dashboard/summary`
- `GET|POST|PUT|DELETE /api/customers`
- `GET|POST|PUT|DELETE /api/measurements`
- `GET|POST|PUT|DELETE /api/orders`
- `GET|POST|PUT|DELETE /api/payments`
- `GET|POST|PUT|DELETE /api/appointments`

Tum API cevaplari su yapi ile doner:

```json
{
  "success": true,
  "message": "Mesaj",
  "data": {}
}
```

## Deployment

### ASP.NET Core publish

```bash
dotnet publish -c Release -o ./publish
```

### Docker ile calistirma

```bash
cd deployment
export TERZITURAN_JWT_KEY="en-az-32-karakterlik-guclu-ve-rastgele-anahtar"
export TERZITURAN_ADMIN_PASSWORD="GucluYoneticiSifresi1!"
docker compose up -d --build
```

Istege bagli ilk yonetici degiskenleri:

- `TERZITURAN_ADMIN_USERNAME`
- `TERZITURAN_ADMIN_FULLNAME`
- `TERZITURAN_ADMIN_EMAIL`

### Nginx reverse proxy

- `deployment/nginx.conf` dosyasi `terzituran.com` ve `terzituran.com.tr` alan adlari icin hazirlanmistir.
- Nginx, istekleri container icindeki ASP.NET Core uygulamasina yonlendirir.

### Domain baglama

- DNS tarafinda `A` veya `CNAME` kayitlarini sunucunuza yonlendirin.
- `terzituran.com`, `www.terzituran.com`, `terzituran.com.tr`, `www.terzituran.com.tr` icin kayit ekleyin.

### Let's Encrypt SSL

- Sunucuda `certbot` kurun.
- Ornek:

```bash
sudo certbot certonly --nginx -d terzituran.com -d www.terzituran.com -d terzituran.com.tr -d www.terzituran.com.tr
```

- Sertifika dizini `docker-compose.yml` icinde `/etc/letsencrypt` olarak mount edilmistir.

### SQLite veritabaninin uretimde tutulmasi

- Uretimde SQLite dosyasini container ici gecici alanda degil, kalici volume veya host klasorunde tutun.
- Bu proje icin varsayilan uretim yolu: `/app/data/terzituran.db`

### Gelecekte Flutter uygulamasi icin API taban adresi

- Ornek production API base URL:
  - `https://terzituran.com/api`
  - veya `https://terzituran.com.tr/api`

## GitHub Kullanim Notlari

- JWT anahtarini `TERZITURAN_JWT_KEY` ortam degiskeniyle saglayin; repoya yazmayin.
- SQLite veritabanini depoya eklemek yerine migration dosyalarini versiyonlayin.
- Commit oncesinde `dotnet build` ve `dotnet ef database update` ile kontrol yapin.

## Juri Sunum Notlari

- Giris ve rol tabanli yetkilendirmeyi gosterin.
- Dashboard kartlari, grafikler ve rapor PDF cikisini canli gosterin.
- Swagger uzerinden JWT login alip korumali endpointleri test edin.
- Musteri detay sayfasinda iliskili olcu, siparis, odeme ve randevu verilerini tek ekranda anlatin.
# Terzi Turan

## Flutter Mobil Uygulama

Mobil uygulama `TerziTuran.Mobile` klasöründedir. Android emülatörde varsayılan API adresi
`http://10.0.2.2:5241`, Windows/macOS çalıştırmalarında `http://127.0.0.1:5241` olarak seçilir.

```powershell
cd TerziTuran.Mobile
flutter pub get
flutter run
```

Farklı bir sunucu adresi kullanmak için:

```powershell
flutter run --dart-define=API_URL=https://api.example.com
```

Üretim paketlerinde mutlaka HTTPS API adresi kullanılmalıdır.
