# 🧠 AI Story Generator (ASP.NET Core MVC, .NET 9)

Create **personalized stories for all age group** and **matching AI-generated illustrations**!
Select or enter an **age group**, **theme**, and **plot**, add optional details, and generate a **3-paragraph HTML story** with **AI-created images** for each paragraph.

---

## 🖼️ Project Screenshot

### 📸 Image 1: Homepage / Input Form

![Homepage Screenshot](docs/screenshot_home.png)

### 🧠 Image 2: Story Generation in Progress (Loader)

![Loader Screenshot](docs/screenshot_loader.png)

### 📖 Image 3: Generated Story Output

![Story Screenshot](docs/screenshot_story.png)

### 🎨 Image 4: AI-Generated Illustrations

![Images Screenshot](docs/screenshot_images.png)

### 💡 Image 5: About Page

![About Screenshot](docs/screenshot_about.png)

### ⚙️ Image 6: API Flow Diagram

![Flow Diagram](docs/architecture_diagram.png)

> Replace the paths above with your actual image URLs or local repo files under `docs/`.

---

## 📚 About

The **Story Generator** is an interactive AI web app built with **ASP.NET Core MVC (.NET 9)**.
It helps parents, teachers, and children create creative, age-appropriate stories and matching illustrations using **Groq**, **Hugging Face**, and **Stability AI**.

💡 Perfect for:

* Bedtime stories
* Classroom creativity sessions
* Fun and educational storytelling activities

---

## 🚀 Tech Stack

| Layer             | Technology                                                  |
| ----------------- | ----------------------------------------------------------- |
| **Backend**       | ASP.NET Core MVC (.NET 9)                                   |
| **HTTP Client**   | `IHttpClientFactory`                                        |
| **Story AI**      | Groq API (Llama 3 70B 8192)                                 |
| **Image AI**      | Hugging Face Inference API → optional Stability AI fallback |
| **Frontend**      | Razor Views, Bootstrap 5, custom JS loader                  |
| **Language**      | C# 12                                                       |
| **Configuration** | `appsettings.json` or User Secrets                          |

---

## ✨ Features

### 🎭 Preset + Custom Inputs

| Field             | Preset Options                                                                                                    | Custom Option             |
| ----------------- | ----------------------------------------------------------------------------------------------------------------- | ------------------------- |
| **Age**           | 3-5, 6-8, 9-12, 13-15, 16-18                                                                                      | User can add their own    |
| **Theme**         | Adventure, Friendship, Magic, Animals, Space, Pirates, Dragons                                                    | User can add their own    |
| **Plot**          | Lost in forest, Treasure hunt, Saving the village, Finding a new friend, Solving a mystery, Winning a competition | User can add their own    |
| **Extra Details** | Free text area                                                                                                    | Optional hints for the AI |

---

### 🧩 AI Story Generation

* Uses **Groq Llama 3 70B 8192** for story text
* Produces structured **HTML** output:

  ```html
  <h2>Story Title</h2>
  <p>Paragraph 1...</p>
  <p>Paragraph 2...</p>
  <p>Paragraph 3...</p>
  ```

---

### 🎨 AI Image Generation

* Builds a visual prompt for each paragraph (`theme + plot + paragraph + details`)
* Attempts multiple **Hugging Face models**, in order:

  1. `stabilityai/stable-diffusion-xl-base-1.0`
  2. `stabilityai/stable-diffusion-2-1`
  3. `runwayml/stable-diffusion-v1-5`
  4. `prompthero/openjourney-v4`
  5. `dreamlike-art/dreamlike-photoreal-2.0`
  6. `CompVis/stable-diffusion-v1-4`
  7. `wavymulder/Analog-Diffusion`
  8. `nitrosocke/Arcane-Diffusion`
  9. `hakurei/waifu-diffusion`
  10. `dgkanatsios/DalleMini`
* If all fail, falls back to **Stability AI** (`stable-diffusion-xl-1024-v1-0`)
* Displays up to **3 images** side-by-side under the story

---

### 💡 Dynamic UI & Loader

* Interactive dropdowns that reveal **custom input fields** when “-- Custom --” is chosen
* Animated **3-step loader overlay**:

  1. Generating story
  2. Generating images
  3. Displaying results

---

## ⚙️ How It Works (Code Overview)

### 🏠 `HomeController`

* **GET / Home / Index**

  * Populates dropdown lists (`ViewBag.Ages`, `ViewBag.Themes`, `ViewBag.Plots`)
* **POST / Home / Index**

  * Reads form input (`age`, `theme`, `plot`, `custom*`, `details`)
  * Builds a prompt and calls `GenerateStoryAsync` (Groq)
  * Extracts `<h2>` for title and up to 3 `<p>` paragraphs
  * For each paragraph:

    * Calls `GenerateImageBase64Async`
    * Tries `TryHuggingFaceModels()` → falls back to `TryStabilityAi()`
  * Returns story HTML and base64 images via `ViewBag`

### 📄 Views

* **Views/Home/Index.cshtml** — Form UI, loader, story viewer, image gallery
* **Views/Home/About.cshtml** — About page explaining the project

---

## 🔑 Configuration

### 🧰 Required API Keys

| Service          | Key                  | Purpose                            |
| ---------------- | -------------------- | ---------------------------------- |
| **Groq**         | `Groq:ApiKey`        | Story text generation              |
| **Hugging Face** | `HuggingFace:ApiKey` | Primary image generation           |
| **Stability AI** | `StabilityAI:ApiKey` | Optional fallback image generation |

### `appsettings.json`

```json
{
  "Groq": {
    "ApiKey": "YOUR_GROQ_API_KEY"
  },
  "HuggingFace": {
    "ApiKey": "YOUR_HUGGINGFACE_API_KEY"
  },
  "StabilityAI": {
    "ApiKey": "YOUR_STABILITY_API_KEY_OPTIONAL"
  }
}
```

### Recommended: Use **User Secrets** in development

```bash
dotnet user-secrets init
dotnet user-secrets set "Groq:ApiKey" "YOUR_GROQ_API_KEY"
dotnet user-secrets set "HuggingFace:ApiKey" "YOUR_HF_API_KEY"
dotnet user-secrets set "StabilityAI:ApiKey" "YOUR_STABILITY_API_KEY"
```

---

## 🧩 Getting Started

### 1️⃣ Clone the repository

```bash
git clone https://github.com/<your-username>/<your-repo>.git
cd <your-repo>
```

### 2️⃣ Restore & Build

```bash
dotnet restore
dotnet build
```

### 3️⃣ Run the app

```bash
dotnet run
```

By default:
➡️ `https://localhost:5001` or `http://localhost:5000`

---

## 🪄 Usage Guide

1. Open the app in your browser
2. Select **Age**, **Theme**, and **Plot**
3. Or choose “-- Custom --” to enter your own values
4. Optionally fill in **Details** to guide the AI
5. Click **Generate Story**
6. Watch the loader steps:

   * Generating story
   * Generating images
   * Displaying results
7. Read your AI-generated story and see the matching images!

---

## 🌐 Environment and APIs

| Component                   | Endpoint / Model                                                                     |
| --------------------------- | ------------------------------------------------------------------------------------ |
| **Groq (LLM)**              | `https://api.groq.com/openai/v1/chat/completions` (model: `llama3-70b-8192`)         |
| **Hugging Face (Images)**   | Multiple models tried sequentially                                                   |
| **Stability AI (Fallback)** | `https://api.stability.ai/v1/generation/stable-diffusion-xl-1024-v1-0/text-to-image` |

---

## ⚠️ Notes & Limitations

* The Groq output must follow `<h2>` + `<p>` HTML format; malformed output may affect parsing.
* Image generation may be slow or return HTTP 503 while models warm up — the code retries automatically.
* Custom options are stored **in static lists** for the app lifetime (reset on restart).
* If all image providers fail, only the story text is shown.
* The script references `generateTitleImageBtn` and `/Home/GenerateTitleImage`, but these are **not implemented**. They can be safely removed or added later.

---

## 🔒 Security

* **Never commit API keys** to source control.
* Use ASP.NET Core **User Secrets** or environment variables in development.
* Use secure secrets management (Azure Key Vault, AWS Secrets Manager, etc.) in production.

---

## 🧰 Troubleshooting

| Issue                      | Solution                                                     |
| -------------------------- | ------------------------------------------------------------ |
| ❌ No story appears         | Check Groq API key and network access                        |
| 🖼️ No images              | Verify Hugging Face API key; optionally add Stability AI key |
| ⏳ Slow or 503 errors       | Models may be loading — retry in 10–15 seconds               |
| 🔄 Custom inputs not saved | Static lists reset after app restart                         |

---

## 🛣️ Roadmap Ideas

* Implement `/Home/GenerateTitleImage` (for cover art)
* Persist user-added inputs (database or session storage)
* Add **model selector** and **negative prompts** for advanced users
* Add **PDF export** or **story gallery**
* Integrate **content safety filters**

---

## 🧾 License

```
MIT License
© 2025 Story Generator Project
```

---

## ❤️ Acknowledgements

* [Groq](https://groq.com/) for ultra-fast Llama 3 inference
* [Hugging Face](https://huggingface.co/) for open model hosting
* [Stability AI](https://stability.ai/) for image fallback
* [Bootstrap](https://getbootstrap.com/) for frontend styling

---
