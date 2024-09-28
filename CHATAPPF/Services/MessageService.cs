using CHATAPPF.Models;

namespace CHATAPPF.Services
{
    public class MessageService : IMessageService
    {
        private readonly ApplicationDbContext _context;

        public MessageService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task StoreMessageAsync(int IDUsuario, int IDComunidad, string message, DateTime FechaEnvio)
        {
            try
            {
                var MensajeChat = new MensajeChat
                {
                    IDUsuario = IDUsuario,
                    IDComunidad = IDComunidad,
                    Mensaje = message,
                    FechaEnvio = FechaEnvio
                };

                _context.MensajeChat.Add(MensajeChat);
                await _context.SaveChangesAsync();
            }
            catch (Exception e)
            {

                Console.WriteLine(e.Message);
            }
            
        }
    }
}
