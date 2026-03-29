# mootoo-Dropper (Advanced Stealth Dropper) 🚀

<p align="center">
  <b>Advanced, Polymorphic, NativeAOT FUD Dropper Generator</b><br>
  <i>Developed by <a href="https://github.com/mootoo11">mootoo11</a></i>
</p>

---

## 🇺🇸 English Documentation

**mootoo-Dropper** is an advanced GUI-based tool designed to generate Highly Stealthy (FUD) payloads that execute malicious scripts (like PowerShell or VBS) entirely in the background while displaying a legitimate decoy file (Lure Document).

### 🔥 Key Features
- **NativeAOT Compilation:** Generates pure Unmanaged Machine Code. It cannot be decompiled by standard .NET tools like dnSpy or ILSpy, destroying static signatures.
- **Hardware Breakpoints (HWBP) for AMSI Bypass:** Dynamically disables AMSI at runtime by manipulating the CPU's `DR0` registers natively using Thread Contexts (No Memory Patching needed!).
- **Parent PID Spoofing:** Dropped payloads are executed seamlessly as child processes of `explorer.exe`, completely breaking the execution chain on EDR telemetry.
- **Dynamic Lure Visibility:** Safely and visibly opens documents like `.zip` or `.pdf` to trick the victim, while the execution of the main payloads remains completely hidden (`SW_HIDE`).
- **Signature Cloning & Timestomping:** Steals the digital signature of `cmd.exe` and rewrites the payload's timestamp to mimic an older, trusted Microsoft binary.

### ⚙️ Prerequisites
To successfully build NativeAOT payloads, the host machine running the builder MUST have the Visual Studio C++ Linker installed:
1. Install [Visual Studio Build Tools](https://visualstudio.microsoft.com/visual-cpp-build-tools/).
2. Select the **"Desktop development with C++"** workload.
3. Ensure the **.NET 8.0/10.0 SDK** is installed on your computer.

### 🛠️ How to Use
1. Run the prepared builder: `mootoo-Dropper.exe`.
2. **Lure File:** Click `...` to select a decoy document (e.g., a `.zip` or `.pdf` file). This is what the victim will see.
3. **Download URLs:** Paste the direct link(s) to your actual payload file(s) that will be fetched silently.
4. Go to the **Finalize** tab, check the generated icon if needed.
5. Click **GENERATE APEX PAYLOAD**, choose where to save your executable, and wait ~15 seconds for the NativeAOT build to finish.

---

## 🇸🇦 الشرح باللغة العربية

**mootoo-Dropper** هو عبارة عن أداة متطورة تم برمجتها لتوليد فيروسات أو برمجيات خبيثة (Droppers) غير قابلة للاكتشاف التام (FUD)، تقوم بتشغيل السكربتات (مثل الباورشيل) في الخلفية بينما يظهر للضحية ملف طبيعي تمويهي.

### 🔥 أهم المميزات
- **بناء NativeAOT**: استخراج الفيروس بلغة الآلة الصافية وتدمير أي إمكانية لعمل هندسة عكسية (Reverse Engineering) باستخدام أدوات مثل dnSpy.
- **تخطي الحماية عبر العتاد (HWBP)**: إيقاف عمل الـ AMSI من خلال التلاعب في مسجلات المعالج (CPU Registers) دون الحاجة للمس خصائص الذاكرة وهو ما يفشل معظم مضادات الفيروسات الحديثة!
- **انتحال هوية الأب (PPID Spoofing)**: تشغيل كافة الملفات من تحت نافذة `explorer.exe` (وكأن المستخدم شغلها يدوياً)، مما يكسر شجرة العمليات في برامج الـ EDR.
- **فتح الـ Lure الذكي**: يتيح خيار عرض ملفات التمويه (مثل الصور، والـ ZIP) بشكل طبيعي ومرئي للضحية، بينما يستمر عمل الفيروس مخفياً تماماً.
- **نسخ الشهادات (Signature Cloning)**: إعطاء الفيروس الخاص بك توقيعاً مطابقاً لتوقيع `cmd.exe` من شركة Microsoft لزيادة فرص تجاوز الفحص.

### ⚙️ المتطلبات
لكي ينجح البرنامج في صناعة الفيروس النهائي بتقنية الـ NativeAOT، **يجب** أن يتوفر في جهازك (جهاز تشغيل برنامج البناء) التالي:
1. تحميل [Visual Studio Build Tools](https://visualstudio.microsoft.com/visual-cpp-build-tools/).
2. أثناء التثبيت، يجب وضع علامة (صح) على حزمة **"Desktop development with C++"**.
3. يجب أن تملك حزمة **.NET SDK** (إصدار 8 أو 10).

### 🛠️ طريقة الاستخدام
1. قم بفتح البرنامج `mootoo-Dropper.exe`.
2. عند خيار **Lure File**: قم بتحديد الملف الوهمي الذي تريد عرضه للضحية (مثلاً ملف `.zip` حقيقي).
3. عند خيار **Download URLs**: ضع الرابط المباشر للـ Payload الرئيسي الخاص بك.
4. انتقل للتبويبة الثالثة **Finalize** واختر أيقونة مناسبة.
5. اضغط على **GENERATE APEX PAYLOAD**، اختر مسار واسم ملف حفظ الـ EXE الجديد، وانتظر 15 ثانية حتى تنتهي عملية دمج وبناء الـ AOT!

<br>

---
> ⚠️ **Disclaimer:** This tool is strictly for educational purposes, Capture The Flag (CTF) events, and authorized Red Teaming exercises. The author assumes no responsibility for any unauthorized usage.
