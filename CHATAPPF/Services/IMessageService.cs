using System.Threading.Tasks;
namespace CHATAPPF.Services
{
    public interface IMessageService
    {
        Task StoreMessageAsync(int IDUsuario, int IDComunidad, string message, DateTime FechaEnvio);
    }
}
