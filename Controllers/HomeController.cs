using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using PensionInge.Models;

namespace PensionInge.Controllers;

public class HomeController : Controller
{
    private readonly IHttpClientFactory _http;
    private readonly IConfiguration _cfg;

    public HomeController(IHttpClientFactory http, IConfiguration cfg)
    {
        _http = http;
        _cfg = cfg;
    }

    public IActionResult Index() => View();
    public IActionResult Restaurace() => View();
    public IActionResult Vylety() => View();
    public IActionResult Galerie() => View();

    [HttpPost("api/chat")]
    public async Task<IActionResult> Chat([FromBody] ChatRequest req)
    {
        if (string.IsNullOrWhiteSpace(req?.Message)) return BadRequest();

        const string system = """
            Jsi hotelový asistent Pensionu Inge ve Vyšším Brodě. Odpovídáš česky, stručně a přátelsky.

            Základní informace:
            - Pension a Restaurace Inge, Mělů 379, 382 73 Vyšší Brod
            - Tel: +420 775 856 372
            - E-mail: info@pensioninge.cz
            - Na břehu řeky Vltavy, 200 m od cisterciáckého kláštera Vyšší Brod
            - 33 km od Českého Krumlova (UNESCO)

            Ubytování (8 pokojů, 27 lůžek ve dvou budovách):
            Pension Inge 1:
            - 2× čtyřlůžkový pokoj s vlastní terasou a výhledem na řeku
            - 1× třílůžkový pokoj
            Pension Inge 2 (100 m od Inge 1):
            - 2× dvoulůžkový pokoj
            - 3× čtyřlůžkový pokoj
            - Zahrada u Vltavy s grilem, kuchyňka, společenská místnost
            Vybavení všech pokojů: WC + koupelna se sprchovým koutem, lednička, TV

            Restaurace:
            - Neděle–čtvrtek: 7:00–20:00, Pátek–sobota: 7:00–22:00
            - Zimní zahrada: 110 míst, Terasy: 80 míst
            - Česká kuchyně: guláš, svíčková, vepřové – čepujeme Budvar
            - Prošla kompletní rekonstrukcí v roce 2021
            - Vhodná pro skupiny, rodinné oslavy, firemní akce
            - Parkování pro 60 aut + 3 autobusy

            Půjčovna lodí:
            - Přímo v penzionu půjčujeme kánoe a raftingové vybavení na Vltavu

            Aktivity v okolí:
            - Klášter Vyšší Brod 200 m – cisterciácké opatství z r. 1259
            - Hrad Rožmberk 10 km
            - Český Krumlov UNESCO 33 km
            - Lyžování: Kramolín 10 km, Sterstein 15 km
            - Aquaworld Lipno 9 km
            - Golf Club Lipno 12 km
            - Turistické a cyklotrasy přímo z penzionu

            Slevy na pobyt: 10 % od 3 nocí, zdarma od 5 nocí.
            Při dotazu na rezervaci nasměruj na formulář na webu nebo na telefon.
            """;

        var body = new
        {
            model = "claude-haiku-4-5-20251001",
            max_tokens = 400,
            system,
            messages = new[] { new { role = "user", content = req.Message } }
        };

        var client = _http.CreateClient();
        client.DefaultRequestHeaders.Add("x-api-key", _cfg["ClaudeApiKey"]);
        client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

        var resp = await client.PostAsync(
            "https://api.anthropic.com/v1/messages",
            new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));

        if (!resp.IsSuccessStatusCode) return StatusCode(500);

        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var text = doc.RootElement.GetProperty("content")[0].GetProperty("text").GetString();
        return Json(new { reply = text });
    }

    [HttpPost("rezervace")]
    public IActionResult Rezervace([FromForm] RezervaceRequest req)
    {
        TempData["Success"] = "Děkujeme! Vaši poptávku jsme přijali. Ozveme se do 24 hodin.";
        return RedirectToAction("Index");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}

public record ChatRequest(string Message);
public record RezervaceRequest(string Jmeno, string Email, string Telefon, string Od, string Do, string TypPokoje, int Osoby, string? Poznamka);
