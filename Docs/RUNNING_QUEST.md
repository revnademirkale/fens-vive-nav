# FENS Navigasyon — Meta Quest ile çalıştırma

Branch: `meta-quest`. Deney akışı, rotalar, koşullar ve loglama `main` ile aynıdır. **Pupil verisi yoktur.**
VIVE talimatları için: `Docs/RUNNING.md` (değişmedi).

İşaretler: **[doğrulandı]** resmi dokümanda var · **[doğrulanmadı]** denerken kontrol edin · **[proje]** bizim kodumuz.

## Bu branch'te ne var (merge conflict olmasın diye)

VIVE dosyalarının **hiçbiri silinmedi veya değiştirilmedi**. Quest için sadece yeni dosyalar eklendi:

| Dosya | Ne yapıyor |
|---|---|
| `Packages/manifest.json` | **Tek değişiklik:** `com.unity.xr.meta-openxr` 1.0.1 satırı eklendi. VIVE paketi duruyor |
| `Assets/Scripts/Quest/QuestPassthroughController.cs` | Meta passthrough. Sahneye eklenmez: Quest ayarı açıksa uygulama açılınca kameraya kendini ekler |
| `Assets/Editor/Quest/FensQuestAndroidXRSetup.cs` | **FENS → Android (Meta Quest)** menüsü: Meta açık, VIVE kapalı |
| `Assets/Editor/Quest/FensQuestBoundarylessManifest.cs` | Quest build'inde sınırı (boundary) kapatır. VIVE build'inde hiçbir şey yapmaz |
| `Docs/RUNNING_QUEST.md` | Bu dosya |

Proje artık **iki gözlüğü de** destekler. Hangi gözlük için build alınacaksa onun menüsüne basılır:

| Gözlük | Menü |
|---|---|
| Meta Quest | **FENS → Android (Meta Quest) → Configure OpenXR for Meta Quest** + **Apply Quest Player settings** |
| VIVE | **FENS → Android → Configure OpenXR for VIVE** + **Apply approved Player settings** |

### ⚠️ Commit kuralı

Menüler bu dosyaları değiştirir. Gözlük değiştirmekten kaynaklanan bu değişiklikleri **commit'lemeyin**
(GitHub Desktop'ta sağ tık → **Discard changes**), yoksa merge'de conflict çıkar:

- `Assets/XR/Settings/Open XR Package Settings.asset`
- `Assets/XR/Settings/OpenXR Editor Settings.asset`
- `ProjectSettings/ProjectSettings.asset` (paket adı)

Meta paketi ilk kurulduğunda Unity'nin oluşturduğu yeni dosyalar (`Assets/XR/Resources/…`, `Assets/XR/UserSimulationSettings/…`,
`Assets/XR/Loaders/SimulationLoader.asset`, `Packages/packages-lock.json`) commit'lenebilir. [proje]

---

## 1. Mac'e kurulacaklar (bir kez)

1. **Android modülleri:** Unity Hub → **Installs** → 2022.3.40f1 → **⚙** → **Add modules** →
   **Android Build Support** + **OpenJDK** + **Android SDK & NDK Tools**.
2. **adb:** Unity → **Settings** → **External Tools** → Android SDK yolu. `platform-tools/adb`.

## 2. Quest için build

1. **File → Build Settings** → **Android** → **Switch Platform**.
2. **FENS → Android (Meta Quest) → Configure OpenXR for Meta Quest** [proje].
   Console'daki `[FENS Quest XR] REPORT`'ta **Must be ON** satırları `[x]`, **Must be OFF** satırları `[ ]` olmalı.
3. **FENS → Android (Meta Quest) → Apply Quest Player settings** → paket adı `com.ens492.fensquestnav`.
4. **File → Save Project**.
5. Gözlükte geliştirici modu açık olmalı (telefondaki Meta Horizon uygulaması → Devices → Headset settings →
   Developer mode) [doğrulanmadı: menü adları değişebilir]. USB-C ile bağlayın, "Allow USB debugging" → **Allow**.
6. **File → Build Settings** → **Run Device**: gözlük → **Build And Run** → `Builds/fens-quest.apk`.
   Console'da `[FENS Quest Boundaryless] Added ...` satırı çıkmalı. `Builds/` git'e eklenmez.

Loglar:
```
adb pull /storage/emulated/0/Android/data/com.ens492.fensquestnav/files/FENS_logs ./FENS_logs_quest
adb logcat -s Unity
```

### Bilinen konular

- **Boundary kapalı** [proje]: Quest build'inde manifest'e `com.oculus.feature.BOUNDARYLESS_APP` eklenir.
  Sadece gözlükteki APK'da çalışır. Sistem önce passthrough'un açık olduğunu kontrol eder.
  Sınır olmadığı için merdiven/kapı gibi yerlerde katılımcının yanında biri olmalı.
  [doğrulandı] [Meta: Boundaryless](https://developers.meta.com/horizon/documentation/unreal/unreal-boundaryless/)
- **Passthrough siyahsa:** `adb logcat -s Unity` içinde `[QuestPassthrough]` satırlarına bakın. OpenXR doğrulaması
  passthrough için Vulkan öneriyor; biz VIVE ile aynı olsun diye OpenGLES3 kullanıyoruz. Gerekirse Vulkan denenir.
- Mac'te Play'de `Failed to load openxr runtime loader` hatası normaldir (bağlı gözlük yok).
- Henüz gözlükte denenmedi.

---

## 3. Laboratuvar test listesi

Testten önce:
- [ ] **Bir başlangıç noktası seçin.** Şu an `Assets/Experiment/TargetSet.asset` içindeki noktalar yer tutucu.
  Hizalamayı test etmek için en az bir hedefin (örneğin **W**) başlangıç noktası, gerçek binada bulabileceğiniz bir
  yere (kapı önü, koridor köşesi) karşılık gelmeli. O noktayı yerde bantla işaretleyin. Bakış yönünü (`startYaw`) de
  bantla bir okla gösterin.
- [ ] Participant Id'yi test için ayarlayın: Hierarchy → **Experiment** → Inspector → **Participant Id** = `LAB01`.
  Bu değer APK'nın içine gömülür; gözlükte değiştirilemez.
- [ ] Gözlük ve iki kumanda şarjlı.

Testler (her madde: ne yapılacak → ne görülmeli):

| # | Test | Ne yapılacak | Ne görülmeli |
|---|---|---|---|
| 1 | Passthrough | Uygulamayı başlatın (gözlükte uygulamayı açın). | Gerçek dünya görünür, ekran siyah değil. Log'da `[QuestPassthrough] Meta passthrough started.` |
| 2 | Kumanda görselleri | Kumandaları elinize alıp gözlükten bakın. | Sanal kumanda modeli, ışın, yazılı ipucu (tooltip) **yok**. Log'da `[ControllerVisibility] Controller visuals HIDDEN` |
| 3 | Kumanda tuşları | Aşağıdaki tuş tablosundaki her kumanda tuşunu bir kez deneyin. | Her basışta bip. Gözlükteki durum yazısında hedef / koşul / model değişir. Tetikler ancak 1 saniye basılı tutunca çalışır. |
| 4 | Model yarı saydam | Sol grip'e (Editor'de **F**) basarak modeli **Transparent** yapın. | Gerçek dünyanın üstünde mavi, yarı saydam bina modeli. |
| 5 | Hizalama | Katılımcı bant işaretinin üstünde, ok yönüne bakarak durur. Doğru hedefi seçin (sağ A/B veya N/B). Sol tetiği 1 s basılı tutun (Editor'de **U**). | Bip. Durum yazısında `aligned (W)`. Mavi sanal duvarlar gerçek duvarların üstüne oturur. Kapı ve köşeler 10–20 cm içinde örtüşmeli (bu eşik bizim tahminimiz, sonucu not edin). |
| 6 | Koridorda kayma | Model Transparent iken koridor boyunca yavaşça yürüyün, sonra geri dönün. | Sanal duvarlar gerçek duvarlarla örtüşmeye devam eder. Yürüdükçe artan bir kayma varsa, kaç metrede kaç cm kaydığını not edin. |
| 7 | Takip kopması (valid) | Modeli **Hidden** yapın. Koşul **None**, sağ tetik 1 s → deneme başlar. Koridoru yürüyün. Sağ tetik 1 s → deneme biter. | Bip ile başlar ve biter. Deneme CSV'sinde `valid` sütunu hep `1` olmalı. `0` olan satırlar, konumun bir karede 3 m'den fazla sıçradığı yerlerdir. |
| 8 | Çizgi | Koşul **VisualOnly**, deneme başlatın. | Yerde soluk mavi-gri rota çizgisi. Çizgi gerçek koridorun ortasında olmalı. |
| 9 | CSV dosyaları | Deneme(ler)den sonra log klasörünü açın (2.5 ile gözlükten kopyalayın). | `FENS_logs/LAB01/` klasöründe: deneme başına `LAB01_trial01_..._.csv`, `LAB01_summary.csv`, `LAB01_alignment.csv`. |
| 10 | Durum yazısı | Deneme dışında ve deneme sırasında gözlükten bakın. | Deneme dışında küçük durum yazısı var. Deneme sırasında yazı **yok**. |

Not alınacaklar: hizalamadaki fark (cm), koridordaki kayma, `valid=0` satır sayısı. Kare hızı akıcı mı? Boundary (sınır) uyarısı çıktı mı?

---

## 4. Tuş tablosu

| Komut | Klavye (Editor) | Meta Quest kumandası | Ne zaman |
|---|---|---|---|
| Sonraki hedef | **N** | Sağ kumanda **A** | Deneme dışında |
| Önceki hedef | **B** | Sağ kumanda **B** | Deneme dışında |
| Sonraki koşul | **P** | Sol kumanda **X** | Deneme dışında |
| Önceki koşul | **O** | Sol kumanda **Y** | Deneme dışında |
| Model görünümü (Hidden → Transparent → Visible) | **F** | Sol kumanda **grip** (yan tuş) | Deneme dışında |
| Hizala | **U** | Sol **tetik**, 1 s basılı tut | Deneme dışında |
| Denemeyi başlat / durdur | **Enter** | Sağ **tetik**, 1 s basılı tut | Her zaman |

- Deneme sırasında kumandada sadece başlat/durdur çalışır. Diğer kumanda tuşları sessizce yok sayılır (katılımcı hata
  bip'i duymasın diye). Klavyede yanlış tuş basılırsa hata bip'i çalar.
- Sağ kumandadaki Meta (sistem) tuşu ve sol kumandadaki menü tuşu kullanılmıyor.
- Mac'te gözlüksüz denemek için (XR Interaction Simulator, Play modunda): **1** = A, **2** = B, **T** = tetik,
  **G** = grip, hepsi **sağ** kumandaya gider. **Shift** basılı tutarken aynı tuşlar **sol** kumandaya gider
  (1 = X, 2 = Y). Simülatör kumanda modunda olmalı; değilse **Tab** ile cihaz modunu değiştirin.
  (Simülatörün tuş dosyasından ve kaynak kodundan okundu.) [proje]

---

## Kaynaklar

- [Unity OpenXR: Meta 1.0](https://docs.unity3d.com/Packages/com.unity.xr.meta-openxr@1.0/manual/index.html)
- [Unity OpenXR: Meta 1.0, Camera (Passthrough)](https://docs.unity3d.com/Packages/com.unity.xr.meta-openxr@1.0/manual/features/camera.html)
- [Meta: Boundaryless](https://developers.meta.com/horizon/documentation/unreal/unreal-boundaryless/)
