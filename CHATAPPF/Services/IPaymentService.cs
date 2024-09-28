using CHATAPPF.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Text;

namespace CHATAPPF.Services
{
    public interface IPaymentService
    {
        void UpdateOrderStatus(string orderId, string status);
        string GetRegistroTarjetaStatus(string token);
        Task<string> RegisterCreditCard(string customerId, string returnUrl); // Añadir este método
        public string ObtenerSuscripciones(string PlanID);
        public string ObtenerDatosCustomer(string customerId);
        Task <string> CreateSubscription(string customerId, string planId);

    }

    public class PaymentService : IPaymentService
    {
        private readonly ApplicationDbContext _context;
        private readonly HttpClient _client = new HttpClient();
        private readonly MSSQLConnector _connector;
        private readonly string _apiKey = "7846952F-415B-40B7-94C0-20L6049C4BCA";
        private readonly string _secretKey = "ae85dbb00102bd85a89895a4c2b186f543ab0fec";
        private readonly string _flowBaseUrl = "https://sandbox.flow.cl/api"; // URL del sandbox
        public async Task<string> InitiatePayment(string orderId, decimal amount, string returnUrl, string confirmationUrl)
        {
            var paymentRequest = new
            {
                apiKey = _apiKey,
                secretKey = _secretKey,
                amount = amount,
                orderId = orderId,
                returnUrl = returnUrl,
                confirmationUrl = confirmationUrl
            };

            var content = new StringContent(JsonConvert.SerializeObject(paymentRequest), Encoding.UTF8, "application/json");
            var response = await _client.PostAsync($"{_flowBaseUrl}/payment/create", content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                dynamic jsonResponse = JsonConvert.DeserializeObject(responseContent);
                return jsonResponse.url; // URL de redirección a la página de pago
            }

            return null;
        }
        public PaymentService(ApplicationDbContext context, MSSQLConnector  connector)
        {
            _context = context;
            _connector = connector;
            var _A = new MSSQLConnector();
        }
        public string GetRegistroTarjetaStatus(string token)
        {
            try
            {
                // Crear los parámetros para la solicitud
                var parameters = new Dictionary<string, string>
        {
            {"apiKey", _apiKey},
            {"token", token}
        };

                // Generar la firma HMAC
                string signature = ComputeHMACSHA256(parameters, _secretKey);
                parameters["s"] = signature;

                // Construir la URL con los parámetros
                var queryString = string.Join("&", parameters.Select(p => $"{p.Key}={p.Value}"));
                var requestUrl = $"{_flowBaseUrl}/customer/getRegisterStatus?{queryString}";

                // Realizar la solicitud GET
                var response = _client.GetAsync(requestUrl).Result;

                if (response.IsSuccessStatusCode)
                {
                    // Leer el contenido de la respuesta
                    var content = response.Content.ReadAsStringAsync().Result;

                    // Insertar el log de éxito
                    _connector.InsertarLog(
                        "GetRegistroTarjetaStatus",
                        "IPAYMENTSERVICE.CS",
                        "SUCCESS en GetRegistroTarjetaStatus",
                        "INFORMACIÓN",
                        $"token: {token}, Content: {content}",
                        0, // Ajusta según corresponda
                        DateTime.Now
                    );

                    return content;
                }
                else
                {
                    // Manejar el caso en que la respuesta no es exitosa
                    var errorResponse = response.Content.ReadAsStringAsync().Result;

                    // Insertar el log de error
                    _connector.InsertarLog(
                        "GetRegistroTarjetaStatus",
                        "IPAYMENTSERVICE.CS",
                        "ERROR en GetRegistroTarjetaStatus",
                        "ERROR",
                        $"Código de estado: {response.StatusCode}, Contenido de error: {errorResponse}, token: {token}",
                        0, // Ajusta según corresponda
                        DateTime.Now
                    );

                    return null;
                }
            }
            catch (Exception ex)
            {
                // Insertar el log de excepción
                _connector.InsertarLog(
                    "GetRegistroTarjetaStatus",
                    "IPAYMENTSERVICE.CS",
                    "ERROR en GetRegistroTarjetaStatus",
                    "ERROR",
                    $"Excepción: {ex.Message}, StackTrace: {ex.StackTrace}, token: {token}",
                    0, // Ajusta según corresponda
                    DateTime.Now
                );

                // Relanzar la excepción para manejarla a un nivel superior si es necesario
                throw;
            }
        }

        public string ObtenerSuscripciones(string PlanID)
        {
            var parameters = new Dictionary<string, string>
        {
            {"apiKey", _apiKey},
            {"planId", PlanID}
        };

            // Generar la firma
            string signature = ComputeHMACSHA256(parameters, _secretKey);
            parameters["s"] = signature;

            // Construir la URL con los parámetros
            var queryString = string.Join("&", parameters.Select(p => $"{p.Key}={p.Value}"));
            var requestUrl = $"{_flowBaseUrl}/subscription/list?{queryString}";

            var response = _client.GetAsync(requestUrl).Result;
            if (response.IsSuccessStatusCode)
            {
                var content = response.Content.ReadAsStringAsync().Result;
                return content;
            }
            else
            {
                // Loguear el error
                // _logger.LogError("Failed to get payment status. Status code: {StatusCode}", response.StatusCode);
                return null;
            }
        }

        public async Task<string> ObtenerSuscripcionesAsync(string planId, int start = 0, int limit = 10, string filter = null, int? status = null)
        {
            var parameters = new Dictionary<string, string>
    {
        {"apiKey", _apiKey},
        {"planId", planId},
        {"start", start.ToString()},
        {"limit", limit.ToString()}
    };

            if (!string.IsNullOrEmpty(filter))
                parameters["filter"] = filter;

            if (status.HasValue)
                parameters["status"] = status.Value.ToString();

            // Generar la firma
            string signature = ComputeHMACSHA256(parameters, _secretKey);
            parameters["s"] = signature;

            // Construir la URL con los parámetros
            var queryString = string.Join("&", parameters.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"));
            var requestUrl = $"{_flowBaseUrl}/subscription/list?{queryString}";

            try
            {
                var response = await _client.GetAsync(requestUrl);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return content;
                }
                else
                {
                    // Manejar el error de la API
                    var errorContent = await response.Content.ReadAsStringAsync();
                    // Aquí podrías deserializar el errorContent para obtener detalles del error
                    throw new Exception($"Error: {response.StatusCode} - {errorContent}");
                }
            }
            catch (Exception ex)
            {
                // Manejar excepciones (por ejemplo, loguear el error)
                throw new Exception($"Exception: {ex.Message}", ex);
            }
        }

        public string ObtenerDatosCustomer(string customerId)
        {
            var parameters = new Dictionary<string, string>
        {
            {"apiKey", _apiKey},
            {"customerId", customerId}
        };

            // Generar la firma
            string signature = ComputeHMACSHA256(parameters, _secretKey);
            parameters["s"] = signature;

            // Construir la URL con los parámetros
            var queryString = string.Join("&", parameters.Select(p => $"{p.Key}={p.Value}"));
            var requestUrl = $"{_flowBaseUrl}/customer/get?{queryString}";

            var response = _client.GetAsync(requestUrl).Result;
            if (response.IsSuccessStatusCode)
            {
                var content = response.Content.ReadAsStringAsync().Result;
                return content;
            }
            else
            {
                // Loguear el error
                // _logger.LogError("Failed to get payment status. Status code: {StatusCode}", response.StatusCode);
                return null;
            }
        }
        public async Task<string> CreateSubscription(string customerId, string planId)
        {
            try
            {
                System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
                string subscriptionStart = DateTime.Now.ToString("yyyy-MM-dd"); // Fecha de inicio de la suscripción

                // Crear los parámetros para la solicitud
                var parameters = new Dictionary<string, string>
        {
            {"apiKey", _apiKey},
            {"planId", planId},
            {"customerId", customerId},
            {"subscription_start", subscriptionStart}
        };

                // Generar la firma HMAC
                parameters["s"] = ComputeHMACSHA256(parameters, _secretKey);

                // Preparar el contenido de la solicitud
                var content = new FormUrlEncodedContent(parameters);

                // Realizar la solicitud POST
                var response = await _client.PostAsync($"{_flowBaseUrl}/subscription/create", content);

                // Verificar el estado de la respuesta
                if (response.IsSuccessStatusCode)
                {
                    // Leer el contenido de la respuesta
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    Console.WriteLine("JSON Response: " + jsonResponse); // Agregar log

                    var jsonObject = JObject.Parse(jsonResponse);
                    string subscriptionId = jsonObject["subscriptionId"].ToString();

                    // Insertar el log de éxito
                    _connector.InsertarLog(
                        "CreateSubscription",
                        "IPAYMENTSERVICE.CS",
                        "SUCCESS en CreateSubscription",
                        "INFORMACIÓN",
                        $"customerId: {customerId}, planId: {planId}, subscriptionId: {subscriptionId}, JSON Response: {jsonResponse}",
                        0, // Ajusta según corresponda
                        DateTime.Now
                    );

                    return subscriptionId;
                }
                else
                {
                    // Manejar el caso en que la respuesta no es exitosa
                    var errorResponse = await response.Content.ReadAsStringAsync();

                    // Insertar el log de error
                    _connector.InsertarLog(
                        "CreateSubscription",
                        "IPAYMENTSERVICE.CS",
                        "ERROR en CreateSubscription",
                        "ERROR",
                        $"Código de estado: {response.StatusCode}, Contenido de error: {errorResponse}, customerId: {customerId}, planId: {planId}",
                        0, // Ajusta según corresponda
                        DateTime.Now
                    );

                    // Lanzar una excepción con detalles del error
                    throw new Exception($"Error en la solicitud: {errorResponse}");
                }
            }
            catch (Exception ex)
            {
                // Insertar el log de excepción
                _connector.InsertarLog(
                    "CreateSubscription",
                    "IPAYMENTSERVICE.CS",
                    "ERROR en CreateSubscription",
                    "ERROR",
                    $"Excepción: {ex.Message}, StackTrace: {ex.StackTrace}, customerId: {customerId}, planId: {planId}",
                    0, // Ajusta según corresponda
                    DateTime.Now
                );

                // Relanzar la excepción para manejarla a un nivel superior si es necesario
                throw;
            }
        }

        public void UpdateOrderStatus(string orderId, string status)
        {
            // Lógica para actualizar el estado del pedido en la base de datos
            //var order = _context.Orders.FirstOrDefault(o => o.OrderId == orderId);
            //if (order != null)
            //{
            //    order.Status = status;
            //    _context.SaveChanges();
            //}
        }
        public async Task<string> RegisterCreditCard(string customerId, string returnUrl)
        {
            try
            {
                // Crear los parámetros para la solicitud
                var parameters = new Dictionary<string, string>
        {
            {"apiKey", _apiKey},
            {"customerId", customerId},
            {"url_return", returnUrl}
        };

                // Generar la firma HMAC
                parameters["s"] = ComputeHMACSHA256(parameters, _secretKey);

                // Preparar el contenido de la solicitud
                var content = new FormUrlEncodedContent(parameters);

                // Realizar la solicitud POST
                var response = await _client.PostAsync($"{_flowBaseUrl}/customer/register", content);

                // Verificar el estado de la respuesta
                if (response.IsSuccessStatusCode)
                {
                    // Leer el contenido de la respuesta
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var jsonResponse = JsonConvert.DeserializeObject<dynamic>(responseContent);

                    // Insertar el log de éxito
                    _connector.InsertarLog(
                        "RegisterCreditCard",
                        "IPAYMENTSERVICE.CS",
                        "OK en RegisterCreditCard",
                        "INFORMACIÓN",
                        $"customerId: {customerId}, URL de registro: {jsonResponse.url}, Token: {jsonResponse.token}",
                        0, // Ajusta según corresponda
                        DateTime.Now
                    );

                    // Devolver la URL con el token
                    return $"{jsonResponse.url}?token={jsonResponse.token}";
                }
                else
                {
                    // Manejar el caso en que la respuesta no es exitosa
                    var errorContent = await response.Content.ReadAsStringAsync();

                    // Insertar el log de error
                    _connector.InsertarLog(
                        "RegisterCreditCard",
                        "IPAYMENTSERVICE.CS",
                        "ERROR en RegisterCreditCard",
                        "ERROR",
                        $"Código de estado: {response.StatusCode}, Contenido de error: {errorContent}, customerId: {customerId}",
                        0, // Ajusta según corresponda
                        DateTime.Now
                    );

                    return null;
                }
            }
            catch (Exception ex)
            {
                // Insertar el log de excepción
                _connector.InsertarLog(
                    "RegisterCreditCard",
                    "IPAYMENTSERVICE.CS",
                    "ERROR en RegisterCreditCard",
                    "ERROR",
                    $"Excepción: {ex.Message}, StackTrace: {ex.StackTrace}, customerId: {customerId}",
                    0, // Ajusta según corresponda
                    DateTime.Now
                );
                return null;
                // Relanzar la excepción para manejarla a un nivel superior si es necesario
                throw;
            }
        }



        private static string ComputeHMACSHA256(Dictionary<string, string> data, string secretKey)
        {
            var encoding = new UTF8Encoding();

            // Ordenar los parámetros alfabéticamente
            var sortedParameters = data.OrderBy(p => p.Key);

            // Construir la cadena a firmar
            var toSign = string.Join("", sortedParameters.Select(p => p.Key + p.Value));

            // Calcular la firma HMAC-SHA256
            var keyBytes = encoding.GetBytes(secretKey);
            var dataBytes = encoding.GetBytes(toSign);

            using (var hmacsha256 = new System.Security.Cryptography.HMACSHA256(keyBytes))
            {
                var hashBytes = hmacsha256.ComputeHash(dataBytes);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }



    }
}
