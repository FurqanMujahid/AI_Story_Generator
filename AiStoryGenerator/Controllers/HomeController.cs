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
    try
    {
        var client = _httpClientFactory.CreateClient();
        var apiKey = _configuration["Groq:ApiKey"];

        if (string.IsNullOrEmpty(apiKey))
        {
            return "Error: Groq API key is not configured.";
        }

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        client.Timeout = TimeSpan.FromSeconds(60);

        // Primary + fallback models known to work with Groq
        var modelsToTry = new[]
        {
            "llama-3.1-70b-versatile",
            "llama-3.1-8b-instant",
            "mixtral-8x7b-32768"
        };

        foreach (var model in modelsToTry)
        {
            var requestBody = new
            {
                model,
                messages = new[]
                {
                    new { role = "user", content = prompt }
                },
                temperature = 0.7,
                max_tokens = 1024
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("https://api.groq.com/openai/v1/chat/completions", content);
            var responseText = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(responseText);
                if (doc.RootElement.TryGetProperty("choices", out var choices) &&
                    choices.GetArrayLength() > 0 &&
                    choices[0].TryGetProperty("message", out var message) &&
                    message.TryGetProperty("content", out var contentElement))
                {
                    return contentElement.GetString() ?? "Error: Empty content returned.";
                }
                return "Error: Invalid response format from Groq API.";
            }
            else
            {
                Console.WriteLine($"Model {model} failed with status {response.StatusCode}");
                Console.WriteLine($"Response: {responseText}");
            }
        }

        return "Error: All Groq model attempts failed.";
    }
    catch (Exception ex)
    {
        return $"Error: {ex.Message}";
    }
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
    
    if (string.IsNullOrEmpty(apiKey))
    {
        Console.WriteLine("Hugging Face API key not configured");
        return null;
    }
    
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
        "dreamlike-art/dreamlike-photoreal-2.0"
    };

    foreach (var model in modelsToTry)
    {
        try
        {
            Console.WriteLine($"Trying Hugging Face model: {model}");
            var url = $"https://api-inference.huggingface.co/models/{model}";
            
            var response = await client.PostAsync(url, content);

            Console.WriteLine($"Model {model} response: {response.StatusCode}");

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
                else
                {
                    Console.WriteLine($"Unexpected content type from {model}: {contentType}");
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Model {model} failed with status {response.StatusCode}: {errorContent}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error with model {model}: {ex.Message}");
        }
        
        // Small delay between model attempts
        await Task.Delay(1000);
    }

    return null;
}
private async Task<string> TryStabilityAi(string prompt)
{
    var client = _httpClientFactory.CreateClient();
    var apiKey = _configuration["StabilityAI:ApiKey"];
    if (string.IsNullOrEmpty(apiKey)) 
    {
        Console.WriteLine("Stability AI API key not configured");
        return null;
    }

    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    client.Timeout = TimeSpan.FromSeconds(60);

    try
    {
        Console.WriteLine("Trying Stability AI API with prompt: " + prompt.Substring(0, Math.Min(50, prompt.Length)) + "...");

        // Updated payload according to Stability AI's current API documentation
       var payload = new
{
    text_prompts = new[]
    {
        new { text = prompt, weight = 1.0 }
    },
    cfg_scale = 7,
    height = 1024, 
    width = 1024,  
    steps = 30,
    samples = 1
};

        var jsonContent = JsonSerializer.Serialize(payload);
        Console.WriteLine("Sending payload: " + jsonContent);

        var response = await client.PostAsync(
            "https://api.stability.ai/v1/generation/stable-diffusion-xl-1024-v1-0/text-to-image",
            new StringContent(jsonContent, Encoding.UTF8, "application/json"));

        Console.WriteLine($"Stability AI response status: {response.StatusCode}");

        if (response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine("Stability AI success response received");
            
            try
            {
                using var doc = JsonDocument.Parse(responseContent);
                var imageBase64 = doc.RootElement
                    .GetProperty("artifacts")[0]
                    .GetProperty("base64")
                    .GetString();
                
                Console.WriteLine("Image generated via Stability AI");
                return imageBase64;
            }
            catch (Exception jsonEx)
            {
                Console.WriteLine($"Error parsing Stability AI response: {jsonEx.Message}");
                Console.WriteLine($"Response content: {responseContent}");
                return null;
            }
        }
        else
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Stability AI failed with status {response.StatusCode}");
            Console.WriteLine($"Error response: {errorContent}");
            return null;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Stability AI error: {ex.Message}");
        if (ex.InnerException != null)
        {
            Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
        }
        return null;
    }
}

}
