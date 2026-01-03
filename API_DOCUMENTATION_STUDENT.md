# O'quvchi API Hujjatlari (Student API Documentation)

**Fayl nomi:** `Controllers/Api/StudentApiController.cs`  
**Base URL:** `https://your-domain.com/api/student`

---

## 🔐 Autentifikatsiya

Barcha API lar (login dan tashqari) Cookie-based autentifikatsiya talab qiladi. Login qilgandan keyin cookie avtomatik saqlanadi.

---

## 📋 Barcha API Endpointlar

### 1. 🔑 Login
**POST** `/api/student/login`

**Request Body:**
```json
{
  "login": "student_login",
  "parol": "password123"
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "userId": 123,
  "fullName": "Ali Valiyev",
  "role": "student"
}
```

**Response (401 Unauthorized):**
```json
{
  "message": "Login yoki parol noto'g'ri."
}
```

---

### 2. 📊 Dashboard Ma'lumotlari
**GET** `/api/student/dashboard`

**Response (200 OK):**
```json
{
  "fullName": "Ali Valiyev",
  "faolGuruhlar": 2,
  "faolKurslar": 3,
  "jamiTolov": 500000,
  "qarzdorlik": 100000,
  "haqdorlik": 50000,
  "oxirgiFoiz": 85.5,
  "tangacha": 15000,
  "tolovlar": [
    {
      "sana": "2025-01-15T10:00:00",
      "kurs": "Matematika",
      "miqdor": 200000,
      "tolovUsuli": "Naqd",
      "qarzdorlik": 0,
      "haqdorlik": 0,
      "chekRaqami": "CHK123",
      "holat": "To'liq"
    }
  ],
  "davomatlar": [
    {
      "sana": "2025-01-20T08:00:00",
      "holati": "Keldi",
      "gruh": "Matematika 1-guruh"
    }
  ],
  "natijalar": [
    {
      "natijaId": 1,
      "imtihonNomi": "1-chorak imtihoni",
      "gruh": "Matematika 1-guruh",
      "sana": "2025-01-10T10:00:00",
      "foiz": 85.5,
      "otdimi": true
    }
  ],
  "gruhlar": [
    {
      "id": 1,
      "nomi": "Matematika 1-guruh",
      "kurs": "Matematika",
      "darsVaqti": "09:00",
      "darsKunlari": "Du,Se,Pa",
      "talabalarSoni": 15
    }
  ],
  "onlineImtihonlar": [
    {
      "id": 1,
      "nomi": "Online test",
      "gruh": "Matematika 1-guruh",
      "kurs": "Matematika",
      "sana": "2025-01-25T10:00:00",
      "muddatDaqiqada": 60,
      "isEligible": true,
      "eligibilityMessage": "Imtihon uchun ruxsat berildi."
    }
  ],
  "kurslar": [
    {
      "nomi": "Matematika",
      "holati": "To'liq",
      "narx": 500000
    }
  ]
}
```

---

### 3. 💳 To'lovlar Tarixi
**GET** `/api/student/payments`

**Response (200 OK):**
```json
{
  "qarzdorlik": 100000,
  "haqdorlik": 50000,
  "items": [
    {
      "id": 1,
      "sana": "2025-01-15T10:00:00",
      "miqdor": 200000,
      "kurs": "Matematika",
      "tolovUsuli": "Naqd",
      "qarzdorlik": 0,
      "haqdorlik": 0,
      "chekRaqami": "CHK123",
      "holat": "To'liq"
    }
  ]
}
```

---

### 4. 📅 Davomat Ro'yxati
**GET** `/api/student/attendance`

**Response (200 OK):**
```json
[
  {
    "id": 1,
    "sana": "2025-01-20T08:00:00",
    "holati": "Keldi",
    "izoh": "Vaqtida keldi",
    "gruh": "Matematika 1-guruh"
  }
]
```

---

### 5. 📊 Baholar Ro'yxati (YANGI)
**GET** `/api/student/grades`

**Response (200 OK):**
```json
[
  {
    "id": 1,
    "sana": "2025-01-20T08:00:00",
    "baho": 95,
    "kursNomi": "Matematika",
    "gruhNomi": "Matematika 1-guruh",
    "mavzu": "Algebra",
    "izoh": "Dars uchun baho: 95%",
    "yaratilganVaqt": "2025-01-20T08:30:00"
  }
]
```

---

### 6. 📋 Imtihon Natijalari Ro'yxati (YANGI)
**GET** `/api/student/results`

**Response (200 OK):**
```json
[
  {
    "id": 1,
    "imtihonId": 5,
    "imtihonNomi": "1-chorak imtihoni",
    "gruh": "Matematika 1-guruh",
    "kurs": "Matematika",
    "sana": "2025-01-10T10:00:00",
    "umumiyBall": 85,
    "maksimalBall": 100,
    "foizNatija": 85.5,
    "otdimi": true,
    "boshlanishVaqti": "2025-01-10T10:00:00",
    "tugashVaqti": "2025-01-10T11:00:00"
  }
]
```

---

### 7. 🧪 Online Imtihonlar Ro'yxati
**GET** `/api/student/exams/online`

**Response (200 OK):**
```json
[
  {
    "id": 1,
    "nomi": "Online test",
    "gruh": "Matematika 1-guruh",
    "kurs": "Matematika",
    "sana": "2025-01-25T10:00:00",
    "boshlanishVaqti": "10:00",
    "tugashVaqti": "11:00",
    "muddatDaqiqada": 60,
    "minimalBall": 50
  }
]
```

---

### 8. 🧪 Imtihon Tafsiloti
**GET** `/api/student/exams/{id}`

**Response (200 OK):**
```json
{
  "id": 1,
  "nomi": "Online test",
  "sana": "2025-01-25T10:00:00",
  "muddatDaqiqada": 60,
  "savollar": [
    {
      "id": 1,
      "savolMatni": "2+2 necha?",
      "variantA": "3",
      "variantB": "4",
      "variantC": "5",
      "variantD": "6",
      "ball": 10
    }
  ]
}
```

---

### 9. 📝 Imtihon Javoblarini Yuborish
**POST** `/api/student/exams/submit`

**Request Body:**
```json
{
  "imtihonId": 1,
  "boshlanishVaqti": "2025-01-25T10:00:00",
  "javoblar": [
    {
      "savolId": 1,
      "tanlanganJavob": "B"
    }
  ]
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "natijaId": 10,
  "umumiyBall": 85,
  "maksimalBall": 100,
  "foiz": 85.0,
  "otdimi": true
}
```

---

### 10. 👤 Profil Ma'lumotlari
**GET** `/api/student/profile`

**Response (200 OK):**
```json
{
  "id": 123,
  "ism": "Ali",
  "familiya": "Valiyev",
  "otasiningIsmi": "Vali o'g'li",
  "login": "ali_valiyev",
  "telefonRaqam": "+998901234567"
}
```

---

### 11. 👤 Profil Yangilash
**POST** `/api/student/profile`

**Request Body:**
```json
{
  "telefonRaqam": "+998901234567",
  "oldPassword": "eski_parol",
  "newPassword": "yangi_parol"
}
```

**Response (200 OK):**
```json
{
  "success": true
}
```

**Response (400 Bad Request):**
```json
{
  "message": "Eski parol noto'g'ri."
}
```

---

### 12. 📄 Imtihon Natijasi Tafsiloti
**GET** `/api/student/results/{natijaId}`

**Response (200 OK):**
```json
{
  "id": 1,
  "imtihonId": 5,
  "imtihon": "1-chorak imtihoni",
  "gruh": "Matematika 1-guruh",
  "kurs": "Matematika",
  "sana": "2025-01-10T10:00:00",
  "umumiyBall": 85,
  "maksimalBall": 100,
  "foizNatija": 85.5,
  "otdimi": true,
  "boshlanishVaqti": "2025-01-10T10:00:00",
  "tugashVaqti": "2025-01-10T11:00:00",
  "javoblar": [
    {
      "savolId": 1,
      "tanlanganJavob": "B",
      "togriJavob": "B",
      "togri": true,
      "ball": 10
    }
  ]
}
```

---

### 13. 🎓 Sertifikatlar Ro'yxati
**GET** `/api/student/certificates`

**Response (200 OK):**
```json
[
  {
    "id": 1,
    "sertifikatRaqami": "CERT-2025-001",
    "sertifikatNomi": "Matematika kursi sertifikati",
    "berilganSana": "2025-01-15T00:00:00",
    "yaroqlilikMuddati": "2026-01-15T00:00:00",
    "ball": 95.5,
    "foiz": 95.5,
    "status": "Faol",
    "imtihon": "1-chorak imtihoni",
    "kurs": "Matematika"
  }
]
```

---

### 14. 🎓 Sertifikat PDF Yuklab Olish
**GET** `/api/student/certificates/{id}/pdf`

**Response:** PDF fayl (binary)

---

## 📱 Android Studio Kotlin Uchun Misol

### 1. Retrofit Interface

```kotlin
interface StudentApiService {
    @POST("api/student/login")
    suspend fun login(@Body request: LoginRequest): Response<LoginResponse>
    
    @GET("api/student/dashboard")
    suspend fun getDashboard(): Response<DashboardResponse>
    
    @GET("api/student/payments")
    suspend fun getPayments(): Response<PaymentsResponse>
    
    @GET("api/student/attendance")
    suspend fun getAttendance(): Response<List<AttendanceItem>>
    
    @GET("api/student/grades")
    suspend fun getGrades(): Response<List<GradeItem>>
    
    @GET("api/student/results")
    suspend fun getResults(): Response<List<ResultItem>>
    
    @GET("api/student/exams/online")
    suspend fun getOnlineExams(): Response<List<OnlineExamItem>>
    
    @GET("api/student/exams/{id}")
    suspend fun getExamDetail(@Path("id") id: Int): Response<ExamDetailResponse>
    
    @POST("api/student/exams/submit")
    suspend fun submitExam(@Body request: SubmitExamRequest): Response<SubmitExamResponse>
    
    @GET("api/student/profile")
    suspend fun getProfile(): Response<ProfileResponse>
    
    @POST("api/student/profile")
    suspend fun updateProfile(@Body request: UpdateProfileRequest): Response<UpdateProfileResponse>
    
    @GET("api/student/results/{natijaId}")
    suspend fun getResultDetail(@Path("natijaId") natijaId: Int): Response<ResultDetailResponse>
    
    @GET("api/student/certificates")
    suspend fun getCertificates(): Response<List<CertificateItem>>
    
    @GET("api/student/certificates/{id}/pdf")
    @Streaming
    suspend fun getCertificatePdf(@Path("id") id: Int): Response<ResponseBody>
}
```

### 2. Data Classlar

```kotlin
data class LoginRequest(
    val login: String,
    val parol: String
)

data class LoginResponse(
    val success: Boolean,
    val userId: Int,
    val fullName: String,
    val role: String
)

data class DashboardResponse(
    val fullName: String,
    val faolGuruhlar: Int,
    val faolKurslar: Int,
    val jamiTolov: Double,
    val qarzdorlik: Double,
    val haqdorlik: Double,
    val oxirgiFoiz: Double?,
    val tangacha: Double,
    val tolovlar: List<PaymentItem>,
    val davomatlar: List<AttendanceItem>,
    val natijalar: List<ExamResultItem>,
    val gruhlar: List<GroupCard>,
    val onlineImtihonlar: List<OnlineExamCard>,
    val kurslar: List<CourseItem>
)

data class GradeItem(
    val id: Int,
    val sana: String,
    val baho: Int,
    val kursNomi: String,
    val gruhNomi: String,
    val mavzu: String,
    val izoh: String?,
    val yaratilganVaqt: String
)

data class ResultItem(
    val id: Int,
    val imtihonId: Int,
    val imtihonNomi: String,
    val gruh: String,
    val kurs: String,
    val sana: String,
    val umumiyBall: Int,
    val maksimalBall: Int,
    val foizNatija: Double,
    val otdimi: Boolean,
    val boshlanishVaqti: String?,
    val tugashVaqti: String?
)
```

### 3. Retrofit Setup

```kotlin
val cookieJar = PersistentCookieJar(
    SetCookieCache(),
    SharedPrefsCookiePersistor(context)
)

val okHttpClient = OkHttpClient.Builder()
    .cookieJar(cookieJar)
    .addInterceptor(HttpLoggingInterceptor().apply {
        level = HttpLoggingInterceptor.Level.BODY
    })
    .build()

val retrofit = Retrofit.Builder()
    .baseUrl("https://your-domain.com/")
    .client(okHttpClient)
    .addConverterFactory(GsonConverterFactory.create())
    .build()

val apiService = retrofit.create(StudentApiService::class.java)
```

---

## ⚠️ Muhim Eslatmalar

1. **Cookie Management:** Login qilgandan keyin cookie avtomatik saqlanadi. Barcha so'rovlar uchun cookie yuborilishi kerak.

2. **Error Handling:** Barcha API lar uchun error handling qo'shing:
   - 401 Unauthorized - Login qayta qiling
   - 400 Bad Request - Noto'g'ri ma'lumot
   - 404 Not Found - Topilmadi
   - 500 Internal Server Error - Server xatosi

3. **Date Format:** Barcha sanalar ISO 8601 formatida: `"2025-01-20T08:00:00"`

4. **Authorization:** Barcha API lar (login dan tashqari) `[Authorize(Roles = "student")]` talab qiladi.

---

## 📝 Fayl Manzili

**API Controller:** `Controllers/Api/StudentApiController.cs`

