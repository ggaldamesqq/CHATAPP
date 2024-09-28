using CHATAPPF.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient.Server;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Net.Http;

namespace CHATAPPF.Controllers
{
    [ApiController]
    [Route("api/payment")]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly ILogger<PaymentController> _logger;
        private readonly MSSQLConnector connector;
        private string API_URL = "http://bdhgdxxbhu.mialerta.cl";
        public PaymentController(IPaymentService paymentService, ILogger<PaymentController> logger)
        {
            _paymentService = paymentService;
            _logger = logger;
            connector = new MSSQLConnector();
        }

        //[HttpPost("confirmation")]
        //public IActionResult Confirmation([FromForm] IDictionary<string, string> formData)
        //{
        //    try
        //    {
        //        // Registrar todos los datos del formulario
        //        if (formData == null || formData.Count == 0)
        //        {
        //            _logger.LogInformation("No form data received.");
        //        }
        //        else
        //        {
        //            foreach (var param in formData)
        //            {
        //                _logger.LogInformation($"Parameter: {param.Key}, Value: {param.Value}");
        //            }
        //        }

        //        if (!formData.TryGetValue("token", out string token) || string.IsNullOrEmpty(token))
        //        {
        //            _logger.LogWarning("Token parameter is missing or null.");
        //            return BadRequest("Invalid form parameters.");
        //        }

        //        var paymentStatus = _paymentService.GetPaymentStatus(token);

        //        if (!string.IsNullOrEmpty(paymentStatus))
        //        {
        //            var jsonResponse = JObject.Parse(paymentStatus);
        //            string status = jsonResponse["status"].ToString();
        //            int estado = status switch
        //            {
        //                "1" => 1, // Transacción pendiente
        //                "2" => 2, // Transacción pagada
        //                "3" => 3, // Transacción fallida
        //                "4" => 4, // Transacción reembolsada
        //                "5" => 5, // Transacción anulada
        //                _ => 0   // Estado desconocido
        //            };

        //            bool resultado = connector.ActualizarEstadoPagoSuscripcion(token, estado);

        //            if (resultado)
        //            {
        //                return Ok("Payment status updated successfully.");
        //            }
        //            else
        //            {
        //                return StatusCode(StatusCodes.Status500InternalServerError, "Failed to update payment status.");
        //            }
        //        }
        //        else
        //        {
        //            return BadRequest("Invalid payment status response.");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error processing payment confirmation.");
        //        return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing the payment confirmation.");
        //    }
        //}

        //[HttpPost("return")]
        //public IActionResult Return([FromForm] IDictionary<string, string> formData)
        //{
        //    try
        //    {
        //        if (!formData.TryGetValue("token", out string token) || string.IsNullOrEmpty(token))
        //        {
        //            _logger.LogWarning("Token parameter is missing or null.");
        //            return BadRequest("Invalid form parameters.");
        //        }

        //        var paymentStatus = _paymentService.GetPaymentStatus(token);

        //        if (!string.IsNullOrEmpty(paymentStatus))
        //        {
        //            // Procesa el estado del pago según tu lógica de negocio
        //            return Redirect($"myapp://payment/success?token={token}");
        //        }
        //        else
        //        {
        //            return Redirect($"myapp://payment/failure?token={token}");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error processing payment return.");
        //        return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing the payment return.");
        //    }
        //}
        [HttpPost("registerCard")]
        public async Task<IActionResult> RegisterCard([FromForm] IDictionary<string, string> formData)
        {
            try
            {
                // Validar parámetros
                if (!formData.TryGetValue("customerId", out string customerId) || string.IsNullOrEmpty(customerId) ||
                    !formData.TryGetValue("returnUrl", out string returnUrl) || string.IsNullOrEmpty(returnUrl))
                {
                    // Insertar log para parámetros faltantes
                    connector.InsertarLog(
                        "RegisterCard",
                        "PaymentController.cs",
                        "ERROR en RegisterCard",
                        "WARNING",
                        "Missing required parameters: customerId or returnUrl",
                        0, // Ajusta según corresponda, por ejemplo, el ID del usuario
                        DateTime.Now
                    );

                    return BadRequest("Missing required parameters.");
                }

                // Registrar la tarjeta
                string registerUrl = await _paymentService.RegisterCreditCard(customerId, returnUrl);
                if (registerUrl != null)
                {
                    // Insertar log para éxito en el registro de la tarjeta
                    connector.InsertarLog(
                        "RegisterCard",
                        "PaymentController.cs",
                        "SUCCESS en RegisterCard",
                        "INFORMACIÓN",
                        $"CustomerId: {customerId}, ReturnUrl: {returnUrl}, RegisterUrl: {registerUrl}",
                        0, // Ajusta según corresponda, por ejemplo, el ID del usuario
                        DateTime.Now
                    );

                    return Ok(new { url = registerUrl });
                }
                else
                {
                    // Insertar log para fallo en el registro de la tarjeta
                    connector.InsertarLog(
                        "RegisterCard",
                        "PaymentController.cs",
                        "ERROR en RegisterCard",
                        "ERROR",
                        $"Failed to register credit card. CustomerId: {customerId}, ReturnUrl: {returnUrl}",
                        0, // Ajusta según corresponda, por ejemplo, el ID del usuario
                        DateTime.Now
                    );

                    return StatusCode(StatusCodes.Status500InternalServerError, "Failed to register credit card.");
                }
            }
            catch (Exception ex)
            {
                // Insertar log para excepción
                connector.InsertarLog(
                    "RegisterCard",
                    "PaymentController.cs",
                    "ERROR en RegisterCard",
                    "ERROR",
                    $"Exception: {ex.Message}, StackTrace: {ex.StackTrace}, CustomerId: {formData["customerId"]}, ReturnUrl: {formData["returnUrl"]}",
                    0, // Ajusta según corresponda, por ejemplo, el ID del usuario
                    DateTime.Now
                );

                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while registering the credit card.");
            }
        }




        [HttpPost("obtenerSuscripciones")]
        public async Task<IActionResult> obtenerSuscripciones([FromForm] IDictionary<string, string> formData)
        {
            if (formData.TryGetValue("planId", out var planId))
            {
                var x = _paymentService.ObtenerSuscripciones(planId);
                return Ok(new { message = "Listado Suscripciones", x });
            }
            else
            {
                return BadRequest(new { message = "planId no encontrado en el formulario" });
            }
        }

        [HttpPost("obtenerDatosCustomer")]
        public async Task<IActionResult> obtenerDatosCustomer([FromForm] IDictionary<string, string> formData)
        {
            if (formData.TryGetValue("customerId", out var customerId))
            {
                var x = _paymentService.ObtenerDatosCustomer(customerId);
                return Ok(new { message = "Listado Suscripciones", x });
            }
            else
            {
                return BadRequest(new { message = "planId no encontrado en el formulario" });
            }
        }
        [HttpGet("success")]
        public IActionResult RegisterCardSuccess([FromQuery] string token)
        {
            // Registro de log para la solicitud fallida
            connector.InsertarLog(
                "RegisterCardFailure",
                "PaymentController.cs",
                "SUCCESS en RegisterCardSuccess string TOKEN",
                "OK",
                "Registro correcto de tarjeta para subscripcion: " + token,
                0, // Ajusta según corresponda, por ejemplo, el ID del usuario
                DateTime.Now
            );
            var html = @"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {
            font-family: Arial, sans-serif;
            display: flex;
            justify-content: center;
            align-items: center;
            height: 100vh;
            background-color: #f0f8ff;
            margin: 0;
        }
        .container {
            text-align: center;
            background: #ffffff;
            padding: 50px;
            border-radius: 10px;
            box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1);
        }
        .container h1 {
            color: #4CAF50;
            font-size: 24px;
            margin-bottom: 20px;
        }
        .container p {
            color: #555555;
            font-size: 18px;
            margin-bottom: 20px;
        }
        .container button {
            display: inline-block;
            padding: 10px 20px;
            color: #ffffff;
            background-color: #4CAF50;
            border: none;
            border-radius: 5px;
            font-size: 16px;
            cursor: pointer;
        }
        .container button:hover {
            background-color: #45a049;
        }
    </style>
    <title>Success</title>
</head>
<body>
    <div class='container'>
        <h1>¡Registro Exitoso!</h1>
        <p>Su tarjeta ha sido registrada exitosamente.</p>
        <button onclick='return false;'>Volver</button>
    </div>
</body>
</html>";
            return Content(html, "text/html");
        }

        [HttpGet("failure")]
        public IActionResult RegisterCardFailure([FromQuery] IDictionary<string, string> formData)
        {
            // Registro de log para la solicitud fallida
            connector.InsertarLog(
                "RegisterCardFailure",
                "PaymentController.cs",
                "ERROR en RegisterCardFailure",
                "ERROR",
                "Se ha producido un fallo en el registro de la tarjeta. Datos de la solicitud: " + string.Join(", ", formData.Select(p => $"{p.Key}: {p.Value}")),
                0, // Ajusta según corresponda, por ejemplo, el ID del usuario
                DateTime.Now
            );

            var html = @"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {
            font-family: Arial, sans-serif;
            display: flex;
            justify-content: center;
            align-items: center;
            height: 100vh;
            background-color: #ffebee;
            margin: 0;
        }
        .container {
            text-align: center;
            background: #ffffff;
            padding: 50px;
            border-radius: 10px;
            box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1);
        }
        .container h1 {
            color: #f44336;
            font-size: 24px;
            margin-bottom: 20px;
        }
        .container p {
            color: #555555;
            font-size: 18px;
            margin-bottom: 20px;
        }
        .container button {
            display: inline-block;
            padding: 10px 20px;
            color: #ffffff;
            background-color: #f44336;
            border: none;
            border-radius: 5px;
            font-size: 16px;
            cursor: pointer;
        }
        .container button:hover {
            background-color: #e53935;
        }
    </style>
    <title>Failure</title>
</head>
<body>
    <div class='container'>
        <h1>Registro Erróneo</h1>
        <p>Hubo un problema al registrar su tarjeta. Por favor, inténtelo de nuevo.</p>
        <button onclick='window.history.back();'>Volver</button>
    </div>
</body>
</html>";

            return Content(html, "text/html");
        }

        [HttpPost("successRegister")]
        public async Task<IActionResult> successRegister([FromForm] IDictionary<string, string> formData)
        {
            try
            {
                if (!formData.TryGetValue("token", out string token) || string.IsNullOrEmpty(token))
                {
                    connector.InsertarLog(
                        "successRegister",
                        "PaymentController.cs",
                        "ERROR en successRegister",
                        "ERROR",
                        "Token parameter is missing or null.",
                        0, // Ajusta según corresponda
                        DateTime.Now
                    );

                    _logger.LogWarning("Token parameter is missing or null.");
                    return BadRequest(new { error = "Invalid form parameters. Token is missing or null." });
                }

                var TarjetaRegistroStatusJson = _paymentService.GetRegistroTarjetaStatus(token);

                if (string.IsNullOrEmpty(TarjetaRegistroStatusJson))
                {
                    connector.InsertarLog(
                        "successRegister",
                        "PaymentController.cs",
                        "ERROR en successRegister",
                        "ERROR",
                        $"Failed to get registration status. Token: {token}",
                        0, // Ajusta según corresponda
                        DateTime.Now
                    );

                    _logger.LogWarning("Failed to get registration status. Token: {Token}", token);
                    return Redirect($"{API_URL}/api/payment/failure?token={token}");
                }

                var paymentStatus = JObject.Parse(TarjetaRegistroStatusJson);
                int status = paymentStatus["status"].Value<int>();

                if (status != 1)
                {
                    connector.InsertarLog(
                        "successRegister",
                        "PaymentController.cs",
                        "ERROR en successRegister",
                        "ERROR",
                        $"Payment status is not successful. Status: {status}, Token: {token}",
                        0, // Ajusta según corresponda
                        DateTime.Now
                    );

                    _logger.LogWarning("Payment status is not successful. Status: {Status}", status);
                    return Redirect($"{API_URL}/api/payment/failure?token={token}");
                }

                // Validación de datos de tarjeta
                string customerId = paymentStatus["customerId"]?.ToString();
                string TipoTarjeta = paymentStatus["creditCardType"]?.ToString();
                int CuatroDigitos = paymentStatus["last4CardDigits"] != null ? Convert.ToInt32(paymentStatus["last4CardDigits"]) : 0;

                if (string.IsNullOrEmpty(customerId) || string.IsNullOrEmpty(TipoTarjeta) || CuatroDigitos == 0)
                {
                    connector.InsertarLog(
                        "successRegister",
                        "PaymentController.cs",
                        "ERROR en successRegister",
                        "ERROR",
                        $"Missing or invalid payment status details. CustomerId: {customerId}, TipoTarjeta: {TipoTarjeta}, CuatroDigitos: {CuatroDigitos}, Token: {token}",
                        0, // Ajusta según corresponda
                        DateTime.Now
                    );

                    _logger.LogWarning("Missing or invalid payment status details. CustomerId: {CustomerId}, TipoTarjeta: {TipoTarjeta}, CuatroDigitos: {CuatroDigitos}", customerId, TipoTarjeta, CuatroDigitos);
                    return Redirect($"{API_URL}/api/payment/failure?token={token}");
                }

                // Obtener IDComunidad
                int IDComunidad = connector.ObtenerIDComunidad_Admin(customerId);
                if (IDComunidad == 0)
                {
                    connector.InsertarLog(
                        "successRegister",
                        "PaymentController.cs",
                        "ERROR en successRegister",
                        "ERROR",
                        $"Invalid IDComunidad for CustomerId: {customerId}, Token: {token}",
                        0, // Ajusta según corresponda
                        DateTime.Now
                    );

                    _logger.LogWarning("Invalid IDComunidad for CustomerId: {CustomerId}", customerId);
                    return Redirect($"{API_URL}/api/payment/failure?token={token}");
                }

                // Actualizar registro de tarjeta
                bool RespuestaActualizarRegistro = connector.ActualizarCustomerFlowRegisterCard(TipoTarjeta, CuatroDigitos, customerId, status, IDComunidad);
                if (!RespuestaActualizarRegistro)
                {
                    connector.InsertarLog(
                        "successRegister",
                        "PaymentController.cs",
                        "ERROR en successRegister",
                        "ERROR",
                        $"Failed to update customer register card. CustomerId: {customerId}, Token: {token}",
                        0, // Ajusta según corresponda
                        DateTime.Now
                    );

                    _logger.LogError("Failed to update customer register card. CustomerId: {CustomerId}", customerId);
                    return Redirect($"{API_URL}/api/payment/failure?token={token}");
                }

                // Obtener datos del cliente
                string customerData = _paymentService.ObtenerDatosCustomer(customerId);
                var customerJson = JObject.Parse(customerData);
                int IDUsuario = Convert.ToInt32(customerJson["externalId"]);
                string email = customerJson["email"]?.ToString();

                if (IDUsuario == 0 || string.IsNullOrEmpty(email))
                {
                    connector.InsertarLog(
                        "successRegister",
                        "PaymentController.cs",
                        "ERROR en successRegister",
                        "ERROR",
                        $"Missing or invalid customer details. CustomerId: {customerId}, IDUsuario: {IDUsuario}, Email: {email}, Token: {token}",
                        0, // Ajusta según corresponda
                        DateTime.Now
                    );

                    _logger.LogWarning("Missing or invalid customer details. CustomerId: {CustomerId}, IDUsuario: {IDUsuario}, Email: {Email}", customerId, IDUsuario, email);
                    return Redirect($"{API_URL}/api/payment/failure?token={token}");
                }

                // Obtener PlanID
                string PlanID = connector.ObtenerPlanID_Admin(customerId, IDComunidad);
                if (string.IsNullOrEmpty(PlanID))
                {
                    connector.InsertarLog(
                        "successRegister",
                        "PaymentController.cs",
                        "ERROR en successRegister",
                        "ERROR",
                        $"Invalid PlanID for CustomerId: {customerId}, IDComunidad: {IDComunidad}, Token: {token}",
                        0, // Ajusta según corresponda
                        DateTime.Now
                    );

                    _logger.LogWarning("Invalid PlanID for CustomerId: {CustomerId}, IDComunidad: {IDComunidad}", customerId, IDComunidad);
                    return Redirect($"{API_URL}/api/payment/failure?token={token}");
                }

                // Crear suscripción
                string IDSubscripcion = await _paymentService.CreateSubscription(customerId, PlanID);
                if (string.IsNullOrEmpty(IDSubscripcion))
                {
                    connector.InsertarLog(
                        "successRegister",
                        "PaymentController.cs",
                        "ERROR en successRegister",
                        "ERROR",
                        $"Failed to create subscription. CustomerId: {customerId}, PlanID: {PlanID}, Token: {token}",
                        0, // Ajusta según corresponda
                        DateTime.Now
                    );

                    _logger.LogError("Failed to create subscription. CustomerId: {CustomerId}, PlanID: {PlanID}", customerId, PlanID);
                    return Redirect($"{API_URL}/api/payment/failure?token={token}");
                }

                // Insertar suscripción
                bool InsertarResultado = connector.InsertarCustomerFlowSuscripcion(customerId, PlanID, IDUsuario, IDComunidad, email, IDSubscripcion);
                if (!InsertarResultado)
                {
                    connector.InsertarLog(
                        "successRegister",
                        "PaymentController.cs",
                        "ERROR en successRegister",
                        "ERROR",
                        $"Failed to insert subscription. CustomerId: {customerId}, PlanID: {PlanID}, IDSubscripcion: {IDSubscripcion}, Token: {token}",
                        0, // Ajusta según corresponda
                        DateTime.Now
                    );

                    _logger.LogError("Failed to insert subscription. CustomerId: {CustomerId}, PlanID: {PlanID}, IDSubscripcion: {IDSubscripcion}", customerId, PlanID, IDSubscripcion);
                    return Redirect($"{API_URL}/api/payment/failure?token={token}");
                }

                // Actualizar total usuarios
                bool Actualizacion_LimiteUsuario = connector.ActualizarTotalUsuarios_Comunidad(IDComunidad.ToString());

                if (!Actualizacion_LimiteUsuario)
                {
                    connector.InsertarLog(
                        "successRegister",
                        "PaymentController.cs",
                        "ERROR en successRegister",
                        "ERROR",
                        $"Failed to update total users in comunidad. IDComunidad: {IDComunidad}, PlanID: {PlanID}, Actualizacion_LimiteUsuario: {Actualizacion_LimiteUsuario}, Token: {token}",
                        0, // Ajusta según corresponda
                        DateTime.Now
                    );

                    _logger.LogError("Failed to update total users in comunidad. IDComunidad: {IDComunidad}, PlanID: {PlanID}, Actualizacion_LimiteUsuario: {Actualizacion_LimiteUsuario}", IDComunidad, PlanID, Actualizacion_LimiteUsuario);
                    return Redirect($"{API_URL}/api/payment/failure?token={token}");
                }

                return Redirect($"{API_URL}/api/payment/success?token={token}");
            }
            catch (Exception ex)
            {
                connector.InsertarLog(
                    "successRegister",
                    "PaymentController.cs",
                    "ERROR en successRegister",
                    "ERROR",
                    $"Exception: {ex.Message}, StackTrace: {ex.StackTrace}",
                    0, // Ajusta según corresponda
                    DateTime.Now
                );

                _logger.LogError(ex, "Error processing payment return.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = "An error occurred while processing the payment return." });
            }
        }



    }

    public class PaymentConfirmation
    {
        public string OrderId { get; set; }
        public string TransactionId { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; }

        public bool IsValid()
        {
            // Implementa la lógica para validar la confirmación
            return !string.IsNullOrEmpty(OrderId) && !string.IsNullOrEmpty(TransactionId) && Amount > 0;
        }
    }
}
