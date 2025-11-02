using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using System.Text.RegularExpressions;


public class HomeController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private static List<string> _customAges = new();
    private static List<string> _customThemes = new();
    private static List<string> _customPlots = new();

    public HomeController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

  [HttpGet]
    public IActionResult Index()
    {
        ViewBag.Ages = new List<string> { "3-5", "6-8", "9-12", "13-15", "16-18" }.Concat(_customAges).ToList();
        ViewBag.Themes = new List<string> { "Adventure", "Friendship", "Magic", "Animals", "Space", "Pirates", "Dragons" }.Concat(_customThemes).ToList();
        ViewBag.Plots = new List<string> { "Lost in forest", "Treasure hunt", "Saving the village", "Finding a new friend", "Solving a mystery", "Winning a competition" }.Concat(_customPlots).ToList();
        return View();
    }

    [HttpPost]
public async Task<IActionResult> Index(string age, string theme, string plot, string customAge, string customTheme, string customPlot, string details)
{
    // Add custom options if provided
    if (!string.IsNullOrEmpty(customAge) && !_customAges.Contains(customAge))
    {
        _customAges.Add(customAge);
    }
    if (!string.IsNullOrEmpty(customTheme) && !_customThemes.Contains(customTheme))
    {
        _customThemes.Add(customTheme);
    }
    if (!string.IsNullOrEmpty(customPlot) && !_customPlots.Contains(customPlot))
    {
        _customPlots.Add(customPlot);
    }

    // Use custom input if selected option is "custom"
    age = age == "custom" ? customAge : age;
    theme = theme == "custom" ? customTheme : theme;
    plot = plot == "custom" ? customPlot : plot;

    ViewBag.Ages = new List<string> { "3-5", "6-8", "9-12", "13-15", "16-18" }.Concat(_customAges).ToList();
    ViewBag.Themes = new List<string> { "Adventure", "Friendship", "Magic", "Animals", "Space", "Pirates", "Dragons" }.Concat(_customThemes).ToList();
    ViewBag.Plots = new List<string> { "Lost in forest", "Treasure hunt", "Saving the village", "Finding a new friend", "Solving a mystery", "Winning a competition" }.Concat(_customPlots).ToList();

    string storyPrompt = $"Write a story for a child aged {age} about {theme}, including a plot about {plot}. ";
    if (!string.IsNullOrWhiteSpace(details))
    {
        storyPrompt += $" Additional details: {details}. ";
    }
    storyPrompt += "Format the story in HTML with a title in <h2> tags and each paragraph in <p> tags. Include exactly 3 paragraphs.";
    string story = await GenerateStoryAsync(storyPrompt);
    ViewBag.Story = story;

    // Extract the title from the generated story
    string storyTitle = "";
    if (!string.IsNullOrEmpty(story))
    {
        var match = Regex.Match(story, "<h2>(.*?)</h2>", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            storyTitle = match.Groups[1].Value;
        }
    }
    if (string.IsNullOrWhiteSpace(storyTitle))
    {
        storyTitle = $"A Story for {age} about {theme}";
    }

    // Extract paragraphs and generate images for each (3 total)
    var paragraphs = Regex.Matches(story, "<p>(.*?)</p>", RegexOptions.IgnoreCase)
        .Cast<Match>()
        .Select(m => m.Groups[1].Value)
        .Take(3) // Ensure we only take 3 paragraphs
        .ToList();

    var paragraphImages = new List<string>();
    foreach (var paragraph in paragraphs)
    {
        // Create a prompt for each paragraph
        string paragraphPrompt = $"{theme} {plot}. {paragraph}";
        if (!string.IsNullOrWhiteSpace(details))
        {
            paragraphPrompt += $". {details}";
        }
        
        string base64Image = await GenerateImageBase64Async(paragraphPrompt);
        paragraphImages.Add("data:image/png;base64," + base64Image);
    }

    ViewBag.ParagraphImages = paragraphImages;

    return View();
}



   private async Task<string> GenerateStoryAsync(string prompt)
    {
        var client = _httpClientFactory.CreateClient();
        var apiKey = _configuration["Groq:ApiKey"];
        var requestBody = new
        {
            model = "llama3-70b-8192",
            messages = new[]
            {
                new { role = "user", content = prompt }
            }
        };
        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        var response = await client.PostAsync("https://api.groq.com/openai/v1/chat/completions", content);
        var responseString = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(responseString);
        var generated = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
        return generated;
    }

    private async Task<string> GenerateImageBase64Async(string prompt)
    {
        var huggingFaceResult = await TryHuggingFaceModels(prompt);
        if (!string.IsNullOrEmpty(huggingFaceResult))
        {
            return huggingFaceResult;
        }

        var stabilityAiResult = await TryStabilityAi(prompt);
        if (!string.IsNullOrEmpty(stabilityAiResult))
        {
            return stabilityAiResult;
        }

        Console.WriteLine("All image generation attempts failed. No image will be shown.");
        return null;
    }

private async Task<string> TryHuggingFaceModels(string prompt)
{
    var client = _httpClientFactory.CreateClient();
    var apiKey = _configuration["HuggingFace:ApiKey"];
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
    client.Timeout = TimeSpan.FromSeconds(60);

    var payload = new
    {
        inputs = prompt,
        options = new { wait_for_model = true }
    };

    var json = JsonSerializer.Serialize(payload);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    var modelsToTry = new[]
    {
        "stabilityai/stable-diffusion-xl-base-1.0",
        "stabilityai/stable-diffusion-2-1",
        "runwayml/stable-diffusion-v1-5",
        "prompthero/openjourney-v4",
        "dreamlike-art/dreamlike-photoreal-2.0",
        "CompVis/stable-diffusion-v1-4",
        "wavymulder/Analog-Diffusion",
        "nitrosocke/Arcane-Diffusion",
        "hakurei/waifu-diffusion",
        "dgkanatsios/DalleMini"
    };

    foreach (var model in modelsToTry)
    {
        try
        {
            Console.WriteLine($"Trying Hugging Face model: {model}");
            var url = $"https://api-inference.huggingface.co/models/{model}";
            var response = await client.PostAsync(url, content);

            if ((int)response.StatusCode == 503)
            {
                var retryAfter = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(15);
                Console.WriteLine($"Model {model} is loading, retrying after {retryAfter.TotalSeconds} seconds...");
                await Task.Delay(retryAfter);
                response = await client.PostAsync(url, content);
            }

            if (response.IsSuccessStatusCode)
            {
                var contentType = response.Content.Headers.ContentType?.MediaType;
                if (contentType?.StartsWith("image/") == true)
                {
                    var bytes = await response.Content.ReadAsByteArrayAsync();
                    Console.WriteLine($"Image generated using model: {model}");
                    return Convert.ToBase64String(bytes);
                }
            }
            Console.WriteLine($"Model {model} failed with status {response.StatusCode}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error with model {model}: {ex.Message}");
        }
    }

    return null;
}

private async Task<string> TryStabilityAi(string prompt)
{
    var client = _httpClientFactory.CreateClient();
    var apiKey = _configuration["StabilityAI:ApiKey"];
    if (string.IsNullOrEmpty(apiKey)) return null;

    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
    client.Timeout = TimeSpan.FromSeconds(60);

    try
    {
        Console.WriteLine("Trying Stability AI API");
        var payload = new
        {
            text_prompts = new[] { new { text = prompt, weight = 1 } },
            cfg_scale = 7,
            height = 512,
            width = 512,
            steps = 30,
            samples = 1
        };

        var response = await client.PostAsync(
            "https://api.stability.ai/v1/generation/stable-diffusion-xl-1024-v1-0/text-to-image",
            new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var imageBase64 = doc.RootElement
                .GetProperty("artifacts")[0]
                .GetProperty("base64")
                .GetString();
            Console.WriteLine("Image generated via Stability AI");
            return imageBase64;
        }
        Console.WriteLine($"Stability AI failed with status {response.StatusCode}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Stability AI error: {ex.Message}");
    }

    return null;
}


}