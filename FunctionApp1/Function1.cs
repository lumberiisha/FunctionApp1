using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text.Json.Serialization;

namespace FunctionApp1
{
    public class Function1
    {
        private readonly ILogger<Function1> _logger;
        private readonly static HttpClient _httpClient=new HttpClient();

        public Function1(ILogger<Function1> logger)
        {
            _logger = logger;
        }

        [Function("Function1")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
            [Microsoft.Azure.Functions.Worker.Http.FromBody] CalculatorRequest person,
            ILogger log)
        {
            // Read the request body as string for debugging purposes
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var requestUrl = "https://secure.ubs.com/public/api/v1/retirement/calculators/tax-saving";

            string jsonData = "";

            if (person.SecondPerson == null)
            {
                 jsonData = $@"{{
    ""annualPayment_n"": {person.AnnualPaymentN},
    ""zipCode"": ""{person.ZipCode}"",
    ""municipality"": ""{person.Municipality}"",
    ""firstPerson"": {{
        ""maritalStatus_cd"": ""{person.FirstPerson.MaritalStatusCd}"",
        ""religion"": ""{person.FirstPerson.Religion}"",
        ""numberOfSupportingChildren_n"": {person.FirstPerson.NumberOfSupportingChildrenN},
        ""grossIncome_n"": {person.FirstPerson.GrossIncomeN}
    }}
}}";
            }
            else
            {
                jsonData = $@"{{
    ""annualPayment_n"": {person.AnnualPaymentN},
    ""zipCode"": ""{person.ZipCode}"",
    ""municipality"": ""{person.Municipality}"",
    ""firstPerson"": {{
        ""maritalStatus_cd"": ""{person.FirstPerson.MaritalStatusCd}"",
        ""religion"": ""{person.FirstPerson.Religion}"",
        ""numberOfSupportingChildren_n"": {person.FirstPerson.NumberOfSupportingChildrenN},
        ""grossIncome_n"": {person.FirstPerson.GrossIncomeN}
    }},
    ""secondPerson"": {{
        ""religion"": ""{person.SecondPerson?.Religion}"",
        ""grossIncome_n"": {person.SecondPerson?.GrossIncomeN}
    }}
}}";

            }



            // Create the HttpRequestMessage
            var request = new HttpRequestMessage(HttpMethod.Post, requestUrl)
            {
                Content = new StringContent(jsonData.Trim(), Encoding.UTF8, "application/json")
            };

            // Add headers to the request
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
            request.Headers.Add("apikey", "ruLyBU9AQB3TxrgqMnA3z6ms0zoxRiqS");
            request.Headers.Add("Accept-Language", "en-CH");
            request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36");
            request.Headers.Add("Origin", "https://secure.ubs.com");
            request.Headers.Add("Referer", "https://secure.ubs.com/app/AF4/FaRetirementFEWeb/pillar-3a-sales/tax-savings?lang=en");

            try
            {
                // Send the request
                HttpResponseMessage response = await _httpClient.SendAsync(request);

                // Check for a successful response
                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    using JsonDocument jsonDoc = JsonDocument.Parse(responseBody);
                    string taxSavings = jsonDoc.RootElement.GetProperty("taxSavings").GetString();

                    return new OkObjectResult(taxSavings);
                }
                else
                {
                    throw new Exception($"Request failed with status code: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                return new ObjectResult($"Error: {ex.Message}")
                {
                    StatusCode = 500 // Internal server error
                };
            }
        }

        public class CalculatorRequest
        {
            [JsonPropertyName("annualPayment_n")]
            public int AnnualPaymentN { get; set; }

            [JsonPropertyName("zipCode")]
            public int ZipCode { get; set; }

            [JsonPropertyName("municipality")]
            public string Municipality { get; set; }

            [JsonPropertyName("firstPerson")]
            public FirstPerson FirstPerson { get; set; }

            [JsonPropertyName("secondPerson")]
            public SecondPerson? SecondPerson { get; set; }
        }

        public class FirstPerson
        {
            [JsonPropertyName("maritalStatus_cd")]
            public string MaritalStatusCd { get; set; }

            [JsonPropertyName("religion")]
            public string Religion { get; set; }

            [JsonPropertyName("numberOfSupportingChildren_n")]
            public int NumberOfSupportingChildrenN { get; set; }

            [JsonPropertyName("grossIncome_n")]
            public int GrossIncomeN { get; set; }
        }

        public class SecondPerson
        {
            [JsonPropertyName("religion")]
            public string Religion { get; set; }

            [JsonPropertyName("grossIncome_n")]
            public int GrossIncomeN { get; set; }
        }
    }
}
