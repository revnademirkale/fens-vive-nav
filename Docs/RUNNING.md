# FENS VIVE Navigasyon — Çalıştırma Talimatları

Bu doküman, deneyi HTC VIVE Focus Vision ile çalıştırmak için adım adım talimattır.
Unity'yi ilk kez kullanan biri için yazıldı.

- **A modu:** Windows PC + VIVE Hub ile kablosuz PC streaming. Unity'de **Play**'e basılır, görüntü gözlüğe gider.
- **B modu:** Mac'te Android APK oluşturulur, gözlüğe yüklenir, uygulama gözlükte tek başına çalışır.

İşaretler:
- **[doğrulandı]**: adım, linki verilen resmi dokümanda var.
- **[doğrulanmadı]**: resmi bir kaynakta bulamadık. Denerken dikkat edin, sonucu buraya yazın.
- **[proje]**: bu projeye özgü adım (kendi kodumuz / menülerimiz).

Kaynak linkleri her adımın yanında ve en altta ([Kaynaklar](#kaynaklar)).

**Testten önce:** Mac'teki son değişiklikler commit'lenip GitHub'a push'lanmış olmalı (branch `real-building`).
Windows PC repoyu oradan alır.

---

## 1. A modu — Windows PC + VIVE Hub (kablosuz PC streaming)

Unity Editor Windows'ta çalışır. **Play**'e basınca görüntü kablosuz olarak gözlüğe gider.
Klavye operatörün elindedir.

### 1.1 PC'ye kurulacaklar (bir kez)

1. **PC gereksinimleri:** Windows 10 20H2 veya üstü, en az GTX 1060 6GB / RX 580, 8 GB RAM, Wi-Fi 5/6/6E router.
   [doğrulandı] [VIVE Streaming gereksinimleri](https://www.vive.com/us/support/vs/category_howto/requirements.html)
2. **Git for Windows:** https://git-scm.com/install/windows adresinden indirip kurun. Varsayılan seçenekler yeterli.
   Projenin VIVE paketi GitHub'dan bir git adresiyle yüklenir. Unity bunun için git 2.14.0 veya üstüne ve git'in
   PATH'te olmasına ihtiyaç duyar.
   [doğrulandı] [Unity: Git dependencies](https://docs.unity3d.com/2022.3/Documentation/Manual/upm-git.html)
   Git'i **Unity'yi açmadan önce** kurun. Unity Hub açıksa kapatıp yeniden açın. Yeniden açmanın gerekli olup
   olmadığı resmi dokümanda yok [doğrulanmadı], ama zararı yok.
3. **Git LFS (şart):** VIVE paketindeki Android kütüphaneleri (`openxr_loader.aar`, `libopenxr_loader.so`,
   `libviveopenxr.so`) ve kumanda modelleri GitHub'da **Git LFS** ile saklanıyor.
   - Git LFS yoksa Unity hata vermez. Bu dosyaların yerine ~130 baytlık "pointer" metin dosyaları indirir.
     [doğrulandı] [Unity: Git dependencies](https://docs.unity3d.com/2022.3/Documentation/Manual/upm-git.html)
   - Mac'te bu yüzden Android build şu hatayla düştü: `zip END header not found` [proje].
   - Git LFS, Git for Windows ile birlikte gelir.
     [doğrulandı] [git-lfs README](https://github.com/git-lfs/git-lfs#installing)
     Kurulumdaki bileşen listesinde "Git LFS (Large File Support)" işaretli kalsın. Bileşenin adı sürüme göre
     değişebilir [doğrulanmadı].
   - Kurulumdan sonra komut satırında (cmd veya Git Bash) şu iki komutu çalıştırın:
     ```
     git lfs version
     git lfs install
     ```
     Birincisi bir sürüm yazmalı (ör. `git-lfs/3.x.x`). `git: 'lfs' is not a git command` yazıyorsa LFS kurulu
     değildir. İkincisi `Git LFS initialized.` yazmalı. Bunu kullanıcı hesabı başına bir kez yapmak yeterli.
     [doğrulandı] [git lfs install](https://github.com/git-lfs/git-lfs/blob/main/docs/man/git-lfs-install.adoc)
   - Bunu da **Unity'yi açmadan önce** yapın. Proje LFS'siz açıldıysa, pointer dosyalar
     `Library/PackageCache/com.htc.upm.vive.openxr@…` klasöründe kalır ve Unity onları kendiliğinden yenilemez.
     Çözüm: Unity'yi kapatın, o klasörü proje dışına taşıyın (silmeyin), Unity'yi yeniden açın.
     Unity paketi yeniden indirir [proje; Mac'te denenecek].
   - Kontrol: `Library/PackageCache/com.htc.upm.vive.openxr@…/Runtime/Android/openxr_loader.aar` yaklaşık
     **1,6 MB** olmalı (1 599 568 bayt). 1 KB'tan küçükse hâlâ pointer'dır [proje].
4. **Unity Hub + Unity 2022.3.40f1:**
   - Unity Hub'ı kurun.
   - Hub → **Installs** → **Install Editor** → **Archive** sekmesi → **download archive** → açılan sayfada
     2022.3.40f1'i bulup **Install** (Unity Hub) linkine tıklayın. Sürüm tam olarak **2022.3.40f1** olmalı.
   - [doğrulandı] [Unity Hub: Add editor](https://docs.unity.com/en-us/hub/add-editor), [Unity arşivi](https://unity.com/releases/editor/archive)
5. **SteamVR:** Steam üzerinden kurun ve bir kez açın.
   [doğrulandı] [PC'yi streaming'e hazırlama](https://www.vive.com/us/support/vs/category_howto/preparing-your-computer-for-streaming.html)
6. **VIVE Hub:** Aynı sayfadaki linkten kurun.
   - Aynı sayfa PC'nin router'a **Ethernet kablosuyla** bağlanmasını ister.
   - NVIDIA kartta "Prefer maximum performance" seçilmesini de ister.
   - [doğrulandı] (aynı link)
7. **SteamVR'ı OpenXR runtime yapın:**
   - HTC'nin destek sayfası: SteamVR → **Settings** → **Advanced Settings: Show** → **Developer** →
     **SET STEAMVR AS OPENXR RUNTIME**.
   - HTC'nin Direct Preview sayfası yolu SteamVR → **Settings** → **OpenXR** olarak veriyor. SteamVR sürümüne göre yer
     değişebilir, ikisinden birinde bulursunuz.
   - [doğrulandı] [VIVE: OpenXR sorunları](https://www.vive.com/us/support/vs/category_howto/trouble-with-openxr-titles.html),
     [VIVE OpenXR Unity: Direct Preview](https://developer.vive.com/resources/openxr/unity/tutorials/direct-preview/)

### 1.2 Repoyu indirme ve açma (bir kez)

1. Komut satırını açın (Başlat → "cmd" veya "Git Bash") ve projeyi koymak istediğiniz klasöre gidin.
2. Şunu çalıştırın:
   ```
   git clone -b real-building https://github.com/revnademirkale/fens-vive-nav.git
   ```
   [doğrulandı] [git clone -b](https://git-scm.com/docs/git-clone)
3. Unity Hub → **Projects** → **Add** (veya **Open**) → az önce oluşan `fens-vive-nav` klasörünü seçin.
   Butonun adı Hub sürümüne göre değişebilir [doğrulanmadı].
4. Projeye tıklayın. **İlk açılış uzun sürer** (paketler indirilir, `Library` klasörü oluşur). Bekleyin.
5. Console'da (Window → General → Console) git veya `com.htc.upm.vive.openxr` ile ilgili kırmızı hata varsa,
   git kurulu değildir ya da PATH'te değildir. 1.1'deki 2. adıma dönün.
   Git LFS kontrolü için 1.1'deki 3. adımın sonundaki boyut kontrolünü yapın (`openxr_loader.aar` ≈ 1,6 MB).

Daha sonra güncellemek için proje klasöründe: `git pull` [proje].

### 1.3 Gözlüğü hazırlama ve PC'ye bağlama (bir kez)

1. Gözlükte oyun alanını (play area) kurun.
   [doğrulandı] [Gözlüğü streaming'e hazırlama](https://www.vive.com/us/support/vs/category_howto/preparing-your-headset-for-streaming.html)
2. PC'de VIVE Hub → **Settings** → **About** → **VIVE Streaming** → "VIVE Streaming (Headset)" yanında **Update**.
   [doğrulandı] (aynı link)
3. Kablosuz eşleştirme: Gözlükte **Settings** → **Connectivity** → **Computer connection** → PC'nizi seçin.
   QR kodu okutun veya iki ekrandaki kodun aynı olduğunu görüp **Pair**'e basın. Bu bir kez yapılır.
   [doğrulandı] [Kablosuz bağlantı](https://www.vive.com/us/support/vs/category_howto/connecting-headset-to-computer-wireless.html)
4. **DisplayPort kullanmayın:** HTC'ye göre DisplayPort bağlantısında MR (passthrough) uygulamaları çalışmaz.
   [doğrulandı] [DP ile MR olmaz](https://www.vive.com/us/support/vs/category_howto/cant-do-mr-with-displayport-connection.html)

### 1.4 Her test gününde: Unity'de Play ile gözlükte çalıştırma

1. VIVE Hub ve SteamVR açık, gözlük PC'ye bağlı (gözlükte **Settings** → **Connectivity** → **Computer connection**).
2. HTC'nin Direct Preview sayfası, gözlükteki streaming uygulamasında **Unity Play Mode**'un açılmasını söylüyor.
   [doğrulandı] [Direct Preview](https://developer.vive.com/resources/openxr/unity/tutorials/direct-preview/)
   Not: O sayfa eski "Vive Business Streaming" adını kullanıyor. Sayfadaki tabloda "PC Streaming" satırı X, Focus Vision
   (AIO) satırı V olarak işaretli. Bu çelişkili görünüyor. Menü adları VIVE Hub sürümünde farklı olabilir [doğrulanmadı].
3. Unity'de **Project** penceresinde `Assets/Scenes/SampleScene`'e çift tıklayın.
4. Kontrol (zaten repoda ayarlı) [proje]:
   - **Edit → Project Settings → XR Plug-in Management** → **Windows (monitör) sekmesi**: **OpenXR** işaretli.
   - Altındaki **OpenXR** sayfasında, Windows sekmesinde: **Enabled Interaction Profiles** listesinde
     **VIVE Focus 3 Controller Interaction** var, **VIVE XR Passthrough** işaretli.
   - HTC'nin passthrough sayfasına göre passthrough, PC streaming'de VIVE OpenXR 2.5.1 ve üstüyle çalışır
     (bizdeki 2.5.1). [doğrulandı] [VIVE Passthrough](https://developer.vive.com/resources/openxr/unity/tutorials/passthrough/)
5. Hierarchy → **Experiment** → Inspector → **Participant Id** yazın (örn. `LAB01`).
6. Üstteki **Play** (▶) butonuna basın.
7. Console'da şunlar görülmeli [proje]:
   - `[XRSimulatorGuard] Real headset active (...): XR Interaction Simulator NOT started in this Play session.`
     Gerçek gözlük varken bilgisayardaki simülatör otomatik kapatılır, yoksa gerçek kafa takibini silerdi.
   - `[Passthrough] Planar passthrough started (underlay).`
   - `[ControllerVisibility] Controller visuals HIDDEN, XRI interactors OFF (...)`
   - `[TrialLogger] Log folder: ...`
8. **Game** penceresine bir kez tıklayın, yoksa klavye tuşları Unity'ye gitmez.
9. Bitince **Play**'e tekrar basıp durdurun.

**Log dosyaları (A modu):** `%userprofile%\AppData\LocalLow\DefaultCompany\fens-vive-nav\FENS_logs\<ID>\`.
Windows Gezgini'nin adres çubuğuna yapıştırın.
[doğrulandı] [Application.persistentDataPath](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Application-persistentDataPath.html)

**Sorun giderme (A modu):**
- **PC'de passthrough siyahsa** (gerçek dünya yerine siyah/gri arka plan):
  - **Neden:** HTC'ye göre URP'de HDR açık ve HDR Precision 32-bit iken, streaming'e gönderilen alfa sıfır olmuyor ve
    passthrough'u kapatıyor. HTC'nin çözümü HDR Precision'ı 64-bit yapmak.
    [doğrulandı] [VIVE OpenXR Unity Troubleshooting, madde 10](https://developer.vive.com/resources/openxr/unity/troubleshooting/)
  - **Bizim mevcut yöntemimiz:** `PassthroughController` kamerada HDR'ı ve post-processing'i kapatıyor. Bunun PC
    streaming'de yeterli olup olmadığını henüz görmedik [doğrulanmadı]. Önce Console'da
    `[Passthrough] Planar passthrough started (underlay).` satırı var mı bakın. Yoksa sorun HDR değil, passthrough
    hiç başlamamış demektir.
  - **Çözüm (sadece siyah görürseniz, Play durdurulmuşken):**
    1. **Edit → Project Settings → Quality**. PC'nin kullandığı seviye **Ultra**. Onun **Render Pipeline Asset**
       alanında `Quality URP Config` yazar. Ona tıklayın. Project penceresinde
       `Assets/Settings/Project Configuration/Quality URP Config` seçilir.
    2. HTC'ye göre gelişmiş ayarları görmek için: Unity → **Settings** (Windows'ta **Edit → Preferences**) →
       **Core Render Pipeline** → **Visibility**: **All Visible**.
    3. Inspector'da **Quality** bölümü → **HDR** işaretli kalsın → **HDR Precision**: **64 Bits**.
    4. **File → Save Project** ile kaydedin, sonra Play'e tekrar basın.
    5. Bu değişiklik repoya yazılır (`Quality URP Config.asset`). İşe yararsa commit'leyin, yaramazsa geri alın
       (32 Bits).
  - 64-bit HDR daha çok bellek ve GPU kullanır (Unity'nin ayar açıklaması). Sadece PC (Ultra) seviyesini etkiler;
    Android başka bir URP asset'i (`Performance URP Config`) kullanır. [proje]
- **Kafa hareket edince görüntü dönmüyor.** Console'da `[XRSimulatorGuard] ... NOT started` satırı var mı bakın. Yoksa:
  **Edit → Project Settings → XR Plug-in Management → XR Interaction Toolkit** → "Use XR Interaction Simulator in
  scenes" kutusunu geçici olarak kaldırın. (Bu ayar repoya da yazılır; Mac'e dönünce geri açın.)

---

## 2. B modu — Mac'ten Android APK, gözlükte tek başına

### 2.1 Mac'e kurulacaklar (bir kez)

1. **Android modülleri:** Unity Hub → **Installs** → 2022.3.40f1'in yanındaki **⚙ (Manage)** → **Add modules** →
   **Android Build Support**'u ve altındaki **OpenJDK** ile **Android SDK & NDK Tools**'u işaretleyin → **Install**.
   Bu Mac'te şu an Android modülü **kurulu değil**. İndirme birkaç GB tutar, diskte yer açın.
   [doğrulandı] [Unity Hub: Add modules](https://docs.unity.com/en-us/hub/add-modules),
   [Android bağımlılıkları](https://docs.unity3d.com/2022.3/Documentation/Manual/android-install-dependencies.html)
2. **adb'nin yeri:** Unity → **Settings** (Preferences) → **External Tools** → **Android** bölümünde SDK yolu yazar.
   adb, o yolun altındaki `platform-tools/adb` dosyasıdır.
   [doğrulandı] [Unity External Tools](https://docs.unity3d.com/2022.3/Documentation/Manual/android-external-tools-reference.html),
   [Android adb](https://developer.android.com/tools/adb)
   Terminal'de kısaca kullanmak için (yolu kendi SDK yolunuzla değiştirin):
   ```
   export PATH="<SDK yolu>/platform-tools:$PATH"
   ```

### 2.2 Projeyi Android için hazırlama

1. Unity'de üst menüden **FENS → Android → Configure OpenXR for VIVE**'a tıklayın [proje].
   - Android tarafında VIVE XR Support, VIVE Focus 3 Controller Interaction ve VIVE XR Passthrough açılır;
     Meta Quest Support kapanır. Ayarlar kaydedilir.
   - Console'da `[FENS Android XR] ANDROID REPORT` başlıklı bir liste çıkar. Orada **Features ON** altında bu üçü,
     **Checked (must be OFF)** altında `[ ] Meta Quest Support` görünmeli.
   - Sadece durumu görmek için: **FENS → Android → Report XR + Player settings** (hiçbir şeyi değiştirmez).
2. **FENS → Android → Apply approved Player settings**'e tıklayın [proje]. Ekip kararıyla (2026-11-11) şunları ayarlar:
   - Minimum API Level: **29**
   - Graphics APIs: sadece **OpenGLES3** (Auto Graphics API kapalı)
   - Default Orientation: **Landscape Left**
   - Package Name: **com.ens492.fensvivenav**

   Console'da her ayar için `önce -> sonra` satırı çıkar. Sonra **File → Save Project**.
   Bu menüyü Android modülü kurulduktan sonra çalıştırın. Değerler zaten doğruysa tekrar çalıştırmak zararsızdır.
   Neden bu değerler: bkz. [Bilinen konular](#bilinen-konular).
3. **File → Build Settings** → listeden **Android**'i seçin → **Switch Platform**. İlk seferde uzun sürer.
   **Scenes In Build** listesinde `Scenes/SampleScene` işaretli olmalı [proje].
   Not: Platform Android'deyken bile Mac'te **Play** aynı çalışır. Editor'ün Play modu her zaman Windows/Mac (Standalone)
   XR ayarlarını kullanır. [proje: XR Management kaynak kodundan]
4. Hierarchy → **Experiment** → **Participant Id**'yi yazın. Bu değer APK'nın içine gömülür.

### 2.3 Gözlükte geliştirici modu ve USB (bir kez)

1. Gözlükte **Settings → Developer options → USB debugging**'i açın.
   [doğrulandı] [HTC Getting Started](https://developer.vive.com/resources/openxr/unity/tutorials/setup-and-installation/getting-started-with-openxr/)
   HTC'nin başka bir sayfası yolu **Settings → Advanced → Developer options** olarak veriyor
   ([VIVE Business](https://business.vive.com/us/support/business-plus/category_howto/enrolling-headsets-via-usb.html)).
   "Developer options"ın önce ayrıca kilidinin açılması gerekip gerekmediği [doğrulanmadı].
2. Gözlüğü USB-C kabloyla Mac'e takın. Gözlükte "USB debugging'e izin ver" sorusu çıkarsa **izin verin**.
3. Mac Terminal'de: `adb devices`. Listede bir satırın sonunda `device` yazmalı.
   `unauthorized` yazıyorsa gözlükteki izin sorusunu onaylayın.
   [doğrulandı] [Android adb](https://developer.android.com/tools/adb)

### 2.4 APK oluşturma ve yükleme

**Yol 1: Build And Run (önerilen).**
1. **File → Build Settings** → **Run Device** listesinden gözlüğü seçin (görünmüyorsa **Refresh**).
2. **Build And Run** → APK için bir dosya adı verin (örn. `Builds/fens.apk`) → bekleyin.
   Bitince uygulama gözlükte kendiliğinden açılır.
   [doğrulandı] [Unity: Android cihazda çalıştırma](https://docs.unity3d.com/2022.3/Documentation/Manual/android-debugging-on-an-android-device.html),
   [Android build süreci](https://docs.unity3d.com/2022.3/Documentation/Manual/android-BuildProcess.html)
   `Builds/` klasörünü git'e eklemeyin.

**Yol 2: Önce Build, sonra adb ile yükleme.**
1. **Build Settings → Build** → `Builds/fens.apk`.
2. Terminal: `adb install -r Builds/fens.apk` (`-r`: eski sürümün üstüne yükler, verileri korur).
   [doğrulandı] [Android adb](https://developer.android.com/tools/adb)

**Uygulamayı gözlükte açma:** Gözlükte **Library** → filtre olarak **Unknown source**.
APK'dan yüklenen VR uygulamaları burada görünür.
[doğrulandı] [VIVE Focus Vision: Library](https://www.vive.com/us/support/focusvision/category_howto/accessing-apps-in-library.html)

**B modunda kumanda:** Klavye yok. Tüm komutlar kumandada (bkz. [Tuş tablosu](#4-tuş-tablosu)).

### 2.5 Log dosyalarını gözlükten Mac'e alma

Android'de `persistentDataPath` = `/storage/emulated/<userid>/Android/data/<paket adı>/files`.
[doğrulandı] [Application.persistentDataPath](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Application-persistentDataPath.html)

Paket adı `com.ens492.fensvivenav` (Player Settings → Other Settings → Package Name).
`<userid>` çoğu cihazda `0` [doğrulanmadı]. Mac Terminal'de:
```
adb pull /storage/emulated/0/Android/data/com.ens492.fensvivenav/files/FENS_logs ./FENS_logs_gozluk
```
Not: Paket adı değişince gözlük bunu **yeni bir uygulama** olarak görür. Eski adla (`com.unity.template.vr`)
yüklenmiş bir test sürümü varsa, onun logları eski klasörde kalır. [proje]
[doğrulandı: `adb pull <uzak> <yerel>`] [Android adb](https://developer.android.com/tools/adb)

Gözlükteki Unity Console mesajlarını (Debug.Log) canlı görmek için: `adb logcat -s Unity` [doğrulanmadı].

### Bilinen konular

- **Minimum API seviyesi.** HTC: "The Android version of Focus Vision is 12"
  ([Troubleshooting, madde 9](https://developer.vive.com/resources/openxr/unity/troubleshooting/)).
  Android 12, API 31 veya 32 (12L) olabilir ([Android sürümleri](https://developer.android.com/tools/releases/platforms)).
  Cihazın API seviyesi uygulamanın minimumundan düşükse Android uygulamayı yüklemez
  ([uses-sdk](https://developer.android.com/guide/topics/manifest/uses-sdk-element)).
  Eskiden 32'ydi; güvenli olsun diye **29**'a indirildi (2026-11-11 ekip kararı).
  Gözlüğün API seviyesini görmek için `adb shell getprop ro.build.version.sdk` [doğrulanmadı].
- **Grafik API'si: OpenGLES3** (eskiden sadece Vulkan'dı).
  - HTC'nin güncel başlangıç dokümanı **OpenGLES**'i seçip Vulkan'ı kaldırmayı söylüyor
    ([Getting Started](https://developer.vive.com/resources/openxr/unity/tutorials/setup-and-installation/getting-started-with-openxr/)).
  - VIVE OpenXR 2.5.1 sürüm notları ikisini de destekliyor
    ([İndirme sayfası](https://developer.vive.com/resources/openxr/unity/download/latest/)).
  - OpenGL ile renk uzayı **Linear** olmalı; projede zaten Linear
    ([Unity OpenXR proje ayarları](https://docs.unity3d.com/Packages/com.unity.xr.openxr@1.14/manual/project-configuration.html)).
- **Ekran yönü: Landscape Left**, HTC'nin başlangıç dokümanındaki gibi (aynı Getting Started linki).
- **Katılımcı ID'si** APK'nın içinde sabit. Her katılımcı için yeniden build gerekir. İleride gözlükte seçilebilir yapılacak.
- **Unity sürümü.** HTC, 2022.3.13f1'deki bir bellek hatası için 2022.3.30f1'i öneriyor. Bizimki 2022.3.40f1, daha yeni.
  ([Troubleshooting, madde 6](https://developer.vive.com/resources/openxr/unity/troubleshooting/))

---

## 3. Pazartesi laboratuvar test listesi

Testten önce:
- [ ] **Bir başlangıç noktası seçin.** Şu an `Assets/Experiment/TargetSet.asset` içindeki noktalar yer tutucu.
  Hizalamayı test etmek için en az bir hedefin (örneğin **W**) başlangıç noktası, gerçek binada bulabileceğiniz bir
  yere (kapı önü, koridor köşesi) karşılık gelmeli. O noktayı yerde bantla işaretleyin. Bakış yönünü (`startYaw`) de
  bantla bir okla gösterin.
- [ ] Participant Id'yi test için ayarlayın: Hierarchy → **Experiment** → Inspector → **Participant Id** = `LAB01`.
  B modunda bu değer APK'nın içine gömülür; gözlükte değiştirilemez.
- [ ] Gözlük ve iki kumanda şarjlı.

Testler (her madde: ne yapılacak → ne görülmeli):

| # | Test | Ne yapılacak | Ne görülmeli |
|---|---|---|---|
| 1 | Passthrough | Uygulamayı başlatın (A: Play, B: gözlükte uygulamayı açın). | Gerçek dünya görünür, ekran siyah değil. Log'da `[Passthrough] Planar passthrough started (underlay).` |
| 2 | Kumanda görselleri | Kumandaları elinize alıp gözlükten bakın. | Sanal kumanda modeli, ışın, yazılı ipucu (tooltip) **yok**. Log'da `[ControllerVisibility] Controller visuals HIDDEN` |
| 3 | Kumanda tuşları | Aşağıdaki tuş tablosundaki her kumanda tuşunu bir kez deneyin. | Her basışta bip. Gözlükteki durum yazısında hedef / koşul / model değişir. Tetikler ancak 1 saniye basılı tutunca çalışır. |
| 4 | Model yarı saydam | Sol grip'e (A modunda **F**) basarak modeli **Transparent** yapın. | Gerçek dünyanın üstünde mavi, yarı saydam bina modeli. |
| 5 | Hizalama | Katılımcı bant işaretinin üstünde, ok yönüne bakarak durur. Doğru hedefi seçin (sağ A/B veya N/B). Sol tetiği 1 s basılı tutun (A modunda **U**). | Bip. Durum yazısında `aligned (W)`. Mavi sanal duvarlar gerçek duvarların üstüne oturur. Kapı ve köşeler 10–20 cm içinde örtüşmeli (bu eşik bizim tahminimiz, sonucu not edin). |
| 6 | Koridorda kayma | Model Transparent iken koridor boyunca yavaşça yürüyün, sonra geri dönün. | Sanal duvarlar gerçek duvarlarla örtüşmeye devam eder. Yürüdükçe artan bir kayma varsa, kaç metrede kaç cm kaydığını not edin. |
| 7 | Takip kopması (valid) | Modeli **Hidden** yapın. Koşul **None**, sağ tetik 1 s → deneme başlar. Koridoru yürüyün. Sağ tetik 1 s → deneme biter. | Bip ile başlar ve biter. Deneme CSV'sinde `valid` sütunu hep `1` olmalı. `0` olan satırlar, konumun bir karede 3 m'den fazla sıçradığı yerlerdir. |
| 8 | Çizgi | Koşul **VisualOnly**, deneme başlatın. | Yerde soluk mavi-gri rota çizgisi. Çizgi gerçek koridorun ortasında olmalı. |
| 9 | CSV dosyaları | Deneme(ler)den sonra log klasörünü açın (A: 1.4 sonundaki klasör; B: 2.5 ile gözlükten kopyalayın). | `FENS_logs/LAB01/` klasöründe: deneme başına `LAB01_trial01_..._.csv`, `LAB01_summary.csv`, `LAB01_alignment.csv`. |
| 10 | Durum yazısı | Deneme dışında ve deneme sırasında gözlükten bakın. | Deneme dışında küçük durum yazısı var. Deneme sırasında yazı **yok**. |

Not alınacaklar: hizalamadaki fark (cm), koridordaki kayma, `valid=0` satır sayısı. A ve B modunda kare hızı akıcı mı?

---

## 4. Tuş tablosu

| Komut | Klavye (A modu) | Kumanda (B modu, A modunda da çalışır) | Ne zaman |
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
- Sağ kumandadaki VIVE (sistem) tuşu ve sol kumandadaki menü tuşu kullanılmıyor.
- Mac'te gözlüksüz denemek için (XR Interaction Simulator, Play modunda): **1** = A, **2** = B, **T** = tetik,
  **G** = grip, hepsi **sağ** kumandaya gider. **Shift** basılı tutarken aynı tuşlar **sol** kumandaya gider
  (1 = X, 2 = Y). Simülatör kumanda modunda olmalı; değilse **Tab** ile cihaz modunu değiştirin.
  (Simülatörün tuş dosyasından ve kaynak kodundan okundu.) [proje]

---

## Kaynaklar

HTC VIVE:
- VIVE OpenXR Unity, Getting Started: https://developer.vive.com/resources/openxr/unity/tutorials/setup-and-installation/getting-started-with-openxr/
- VIVE OpenXR Unity, Direct Preview: https://developer.vive.com/resources/openxr/unity/tutorials/direct-preview/
- VIVE OpenXR Unity, Passthrough: https://developer.vive.com/resources/openxr/unity/tutorials/passthrough/
- VIVE OpenXR Unity, Troubleshooting: https://developer.vive.com/resources/openxr/unity/troubleshooting/
- VIVE OpenXR Unity, indirme / sürüm notları: https://developer.vive.com/resources/openxr/unity/download/latest/
- VIVE Streaming gereksinimleri: https://www.vive.com/us/support/vs/category_howto/requirements.html
- PC'yi hazırlama: https://www.vive.com/us/support/vs/category_howto/preparing-your-computer-for-streaming.html
- Gözlüğü hazırlama: https://www.vive.com/us/support/vs/category_howto/preparing-your-headset-for-streaming.html
- Kablosuz bağlantı: https://www.vive.com/us/support/vs/category_howto/connecting-headset-to-computer-wireless.html
- DisplayPort ile MR olmaz: https://www.vive.com/us/support/vs/category_howto/cant-do-mr-with-displayport-connection.html
- OpenXR runtime (SteamVR): https://www.vive.com/us/support/vs/category_howto/trouble-with-openxr-titles.html
- Focus Vision Library (Unknown source): https://www.vive.com/us/support/focusvision/category_howto/accessing-apps-in-library.html
- Focus Vision'a APK yükleme (adb'siz yol): https://www.vive.com/us/support/focusvision/category_howto/installing-apk-on-headset.html

Unity:
- Git dependencies: https://docs.unity3d.com/2022.3/Documentation/Manual/upm-git.html
- Hub, editor ekleme: https://docs.unity.com/en-us/hub/add-editor ve arşiv: https://unity.com/releases/editor/archive
- Hub, modül ekleme: https://docs.unity.com/en-us/hub/add-modules
- Android bağımlılıkları: https://docs.unity3d.com/2022.3/Documentation/Manual/android-install-dependencies.html
- External Tools (Android SDK yolu): https://docs.unity3d.com/2022.3/Documentation/Manual/android-external-tools-reference.html
- Android cihazda çalıştırma: https://docs.unity3d.com/2022.3/Documentation/Manual/android-debugging-on-an-android-device.html
- Android build süreci: https://docs.unity3d.com/2022.3/Documentation/Manual/android-BuildProcess.html
- Android Player ayarları: https://docs.unity3d.com/2022.3/Documentation/Manual/class-PlayerSettingsAndroid.html
- Application.persistentDataPath: https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Application-persistentDataPath.html
- OpenXR proje ayarları: https://docs.unity3d.com/Packages/com.unity.xr.openxr@1.14/manual/project-configuration.html

Android / git:
- adb: https://developer.android.com/tools/adb
- Android API seviyeleri: https://developer.android.com/tools/releases/platforms
- uses-sdk (minSdkVersion): https://developer.android.com/guide/topics/manifest/uses-sdk-element
- Git for Windows: https://git-scm.com/install/windows
- Git LFS (kurulum, Git for Windows'ta dahil): https://github.com/git-lfs/git-lfs#installing
- git lfs install: https://github.com/git-lfs/git-lfs/blob/main/docs/man/git-lfs-install.adoc
- git clone: https://git-scm.com/docs/git-clone
