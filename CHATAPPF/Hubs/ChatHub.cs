using CHATAPPF.Services;
using Microsoft.AspNetCore.SignalR;
using System.Data;

namespace CHATAPPF.Hubs
{
    public class ChatHub : Hub
    {

        private readonly IMessageService _messageService;

        public ChatHub(IMessageService messageService)
        {
            _messageService = messageService;
        }
        //public async Task Send(int IDUsuario, int IDComunidad, string NombreDireccion, string message, DateTime FechaEnvio,string NumeroTelefonico)
        //{
        //    try
        //    {
        //        await _messageService.StoreMessageAsync(IDUsuario, IDComunidad, message, FechaEnvio);

        //        await Clients.All.SendAsync("Receive", IDUsuario, NombreDireccion, IDComunidad, message, FechaEnvio,NumeroTelefonico);
        //    }
        //    catch (Exception e)
        //    {
        //        Console.WriteLine(e.Message);
        //    }
        //}
        public async Task Send(int IDUsuario, int IDComunidad, string NombreDireccion, string message, DateTime FechaEnvio, string NumeroTelefonico)
        {
            try
            {
                await _messageService.StoreMessageAsync(IDUsuario, IDComunidad, message, FechaEnvio);

                // Enviar el mensaje solo al grupo específico (IDComunidad)
                await Clients.Group(IDComunidad.ToString()).SendAsync("Receive", IDUsuario, NombreDireccion, IDComunidad, message, FechaEnvio, NumeroTelefonico);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
        public Task JoinRoom(string roomName)
        {
            return Groups.AddToGroupAsync(Context.ConnectionId, roomName);
        }
        public async Task LeaveRoom(int IDComunidad)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, IDComunidad.ToString());
        }
        public override async Task OnConnectedAsync()
        {
            // El IDComunidad debería ser enviado desde el cliente cuando se conecte
            // Por ejemplo, a través de una query string o un parámetro de conexión
            var IDComunidad = Context.GetHttpContext().Request.Query["IDComunidad"];
            await Groups.AddToGroupAsync(Context.ConnectionId, IDComunidad);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var IDComunidad = Context.GetHttpContext().Request.Query["IDComunidad"];
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, IDComunidad);
            await base.OnDisconnectedAsync(exception);
        }
    }
}
