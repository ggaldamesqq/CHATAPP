using Microsoft.Data.SqlClient;

namespace CHATAPPF.Services
{
    public class MSSQLConnector        
    {
        //private string connectionString = "Data Source=192.168.4.24,1433;Initial Catalog=SOS;User ID=sa;Password=4321Xalito.";
        private string connectionString = "Data Source=190.107.176.16;Initial Catalog=mialerta_;User ID=ggaldamesq;Password=4321Xalito.";
        string p = "Server=190.107.176.16;Database=mialerta_;User Id=ggaldamesq;Password=4321Xalito.;";

        public bool ActualizarEstadoPagoSuscripcion(string token, int estado)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string query = @"
            UPDATE PagoSuscripcion
            SET Estado = @Estado
            WHERE Token = @Token";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Token", token);
                        command.Parameters.AddWithValue("@Estado", estado);

                        int rowsAffected = command.ExecuteNonQuery();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al actualizar el estado del pago de suscripción: " + ex.Message);
                return false;
            }
        }

        public int ObtenerIDComunidad_Admin(string customer)
        {
            int idComunidad = 0;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string query = @"
            SELECT IDComunidad
            FROM CustomerFlowRegisterCard
            WHERE CustomerID = @CustomerID
              AND FechaCreacion = (
                  SELECT MAX(FechaCreacion)
                  FROM CustomerFlowRegisterCard
                  WHERE CustomerID = @CustomerID
              );";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@CustomerID", customer);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            idComunidad = Convert.ToInt32(reader["IDComunidad"]);
                        }
                    }
                }
            }

            return idComunidad;
        }


        public string ObtenerPlanID_Admin(string customer, int IDComunidad)
        {
            string PlanID = "";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string query = @"
            SELECT PlanID
            FROM CustomerFlowRegisterCard
            WHERE CustomerID = @CustomerID
              AND FechaCreacion = (
                  SELECT MAX(FechaCreacion)
                  FROM CustomerFlowRegisterCard
                  WHERE CustomerID = @CustomerID
                  AND IDComunidad = @IDComunidad
              );";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@CustomerID", customer);
                    command.Parameters.AddWithValue("@IDComunidad", IDComunidad);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            PlanID = reader["PlanID"].ToString();
                        }
                    }
                }
            }

            return PlanID;
        }


        public bool ActualizarCustomerFlowRegisterCard(string TipoTarjeta, int CuatroDigitos, string CustomerID, int Estado, int IDComunidad)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string query = @"
                    UPDATE CustomerFlowRegisterCard
                    SET EstadoRegistro = @Estado, CuatroDigitos = @CuatroDigitos, TipoTarjetaCredito = @TipoTarjeta
                    WHERE CustomerID = @CustomerID
                    AND IDComunidad = @IDComunidad";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@CustomerID", CustomerID);
                        command.Parameters.AddWithValue("@Estado", Estado);
                        command.Parameters.AddWithValue("@CuatroDigitos", CuatroDigitos);
                        command.Parameters.AddWithValue("@TipoTarjeta", TipoTarjeta);
                        command.Parameters.AddWithValue("@IDComunidad", IDComunidad);


                        int rowsAffected = command.ExecuteNonQuery();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al actualizar el estado de registro tarjeta crédito ActualizarCustomerFlowRegisterCard: " + ex.Message);
                return false;
            }
        }


        public bool InsertarCustomerFlowSuscripcion(string CustomerID, string PlanID, int IDUsuario, int IDComunidad, string Correo, string IDSuscripcionFlow)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string query = @"
                INSERT INTO CustomerFlowSuscripcion (
                    IDSuscripcionFlow,
                    CustomerID, 
                    EstadoRegistro, 
                    FechaCreacion, 
                    PlanID,
                    IDComunidad,
                    IDUsuario,
                    Correo
                ) 
                VALUES (
                    @IDSuscripcionFlow,
                    @CustomerID, 
                    1, 
                    GETDATE(),
                    @PlanID,
                    @IDComunidad,
                    @IDUsuario,
                    @Correo
                );";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@IDSuscripcionFlow", IDSuscripcionFlow);
                        command.Parameters.AddWithValue("@CustomerID", CustomerID);
                        command.Parameters.AddWithValue("@PlanID", PlanID);
                        command.Parameters.AddWithValue("@IDUsuario", IDUsuario);
                        command.Parameters.AddWithValue("@IDComunidad", IDComunidad);
                        command.Parameters.AddWithValue("@Correo", Correo);

                        int rowsAffected = command.ExecuteNonQuery();

                        // Insertar log de éxito
                        if (rowsAffected > 0)
                        {
                            InsertarLog(
                                "InsertarCustomerFlowSuscripcion",
                                "TuArchivoDeCodigo.cs", // Reemplaza con el nombre real del archivo
                                "INFO",
                                "INFO",
                                $"Suscripción insertada con éxito. CustomerID: {CustomerID}, PlanID: {PlanID}, IDUsuario: {IDUsuario}, IDComunidad: {IDComunidad}, Correo: {Correo}, IDSuscripcionFlow: {IDSuscripcionFlow}",
                                0, // Ajusta según corresponda
                                DateTime.Now
                            );

                            return true;
                        }
                        else
                        {
                            // Insertar log de advertencia si no se insertaron filas
                            InsertarLog(
                                "InsertarCustomerFlowSuscripcion",
                                "TuArchivoDeCodigo.cs", // Reemplaza con el nombre real del archivo
                                "WARN",
                                "Advertencia",
                                $"No se insertaron filas para la suscripción. CustomerID: {CustomerID}, PlanID: {PlanID}, IDUsuario: {IDUsuario}, IDComunidad: {IDComunidad}, Correo: {Correo}, IDSuscripcionFlow: {IDSuscripcionFlow}",
                                0, // Ajusta según corresponda
                                DateTime.Now
                            );

                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Insertar log de error en caso de excepción
                InsertarLog(
                    "InsertarCustomerFlowSuscripcion",
                    "TuArchivoDeCodigo.cs", // Reemplaza con el nombre real del archivo
                    "ERROR",
                    "ERROR",
                    $"Error al insertar la suscripción: {ex.Message}, StackTrace: {ex.StackTrace}. CustomerID: {CustomerID}, PlanID: {PlanID}, IDUsuario: {IDUsuario}, IDComunidad: {IDComunidad}, Correo: {Correo}, IDSuscripcionFlow: {IDSuscripcionFlow}",
                    0, // Ajusta según corresponda
                    DateTime.Now
                );

                Console.WriteLine("Error al insertar el pago de suscripción: " + ex.Message);
                return false;
            }
        }

        public void InsertarLog(string funcion, string texto, string categoria, string respuesta, string campo1, int idUsuario, DateTime fechaCreacion)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string query = "INSERT INTO Log (Funcion, Texto, Categoria, Respuesta, Campo1, IDUsuario, FechaCreacion) " +
                                   "VALUES (@Funcion, @Texto, @Categoria, @Respuesta, @Campo1, @IDUsuario, @FechaCreacion)";
                    SqlCommand command = new SqlCommand(query, connection);

                    command.Parameters.AddWithValue("@Funcion", funcion);
                    command.Parameters.AddWithValue("@Texto", texto);
                    command.Parameters.AddWithValue("@Categoria", categoria);
                    command.Parameters.AddWithValue("@Respuesta", respuesta);
                    command.Parameters.AddWithValue("@Campo1", campo1);
                    command.Parameters.AddWithValue("@IDUsuario", idUsuario);
                    command.Parameters.AddWithValue("@FechaCreacion", fechaCreacion);

                    command.ExecuteNonQuery();  // Ejecutar la consulta de inserción

                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }
        public bool ActualizarTotalUsuarios_Comunidad(string IDCOMUNIDAD)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string query = @"
                DECLARE @v_PlanID INT;
                DECLARE @ErrorMsg NVARCHAR(100);

                -- VALIDACION EXISTENCIA DE PAGO REALIZADO CORRECTAMENTE
                SELECT @v_PlanID = PlanID
                FROM CustomerFlowSuscripcion
                WHERE IDComunidad = @IDCOMUNIDAD
                  AND EstadoRegistro = 1;

                -- Verificar si se encontró un PlanID válido
                IF @v_PlanID IS NULL
                BEGIN
                    SET @ErrorMsg = 'Error de estado de suscripción: No se encontró un PlanID válido para la comunidad especificada.';
                    THROW 51000, @ErrorMsg, 1;
                END

                -- Actualizar ComunidadLimiteClientes con el nuevo límite de usuarios
                UPDATE ComunidadLimiteClientes
                SET LimiteUsuarios = (
                    SELECT UsuariosLimite
                    FROM Suscripcion
                    WHERE Precio = @v_PlanID
                )
                WHERE IDComunidad = @IDCOMUNIDAD;
            ";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@IDCOMUNIDAD", IDCOMUNIDAD);

                        int rowsAffected = command.ExecuteNonQuery();

                        // Insertar log de éxito
                        if (rowsAffected > 0)
                        {
                            InsertarLog(
                                "ActualizarTotalUsuarios_Comunidad",
                                "TuArchivoDeCodigo.cs", // Reemplaza con el nombre real del archivo
                                "INFO",
                                "INFO",
                                $"Número total de usuarios actualizado exitosamente para la comunidad ID: {IDCOMUNIDAD}. Filas afectadas: {rowsAffected}.",
                                0, // Ajusta según corresponda
                                DateTime.Now
                            );

                            return true;
                        }
                        else
                        {
                            // Insertar log de advertencia si no se afectaron filas
                            InsertarLog(
                                "ActualizarTotalUsuarios_Comunidad",
                                "TuArchivoDeCodigo.cs", // Reemplaza con el nombre real del archivo
                                "WARN",
                                "Advertencia",
                                $"No se actualizaron filas para la comunidad ID: {IDCOMUNIDAD}. Filas afectadas: {rowsAffected}.",
                                0, // Ajusta según corresponda
                                DateTime.Now
                            );

                            return false;
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                // Insertar log de error para excepciones SQL
                if (ex.Number == 51000)
                {
                    InsertarLog(
                        "ActualizarTotalUsuarios_Comunidad",
                        "TuArchivoDeCodigo.cs", // Reemplaza con el nombre real del archivo
                        "ERROR",
                        "ERROR",
                        $"Error específico SQL: {ex.Message}. Comunidad ID: {IDCOMUNIDAD}.",
                        0, // Ajusta según corresponda
                        DateTime.Now
                    );
                }
                else
                {
                    InsertarLog(
                        "ActualizarTotalUsuarios_Comunidad",
                        "TuArchivoDeCodigo.cs", // Reemplaza con el nombre real del archivo
                        "ERROR",
                        "ERROR",
                        $"Error al actualizar el total de usuarios para la comunidad ID: {IDCOMUNIDAD}. Error SQL: {ex.Message}.",
                        0, // Ajusta según corresponda
                        DateTime.Now
                    );
                }

                Console.WriteLine("Error al actualizar el total de usuarios de la comunidad: " + ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                // Insertar log de error para excepciones generales
                InsertarLog(
                    "ActualizarTotalUsuarios_Comunidad",
                    "TuArchivoDeCodigo.cs", // Reemplaza con el nombre real del archivo
                    "ERROR",
                    "ERROR",
                    $"Error al actualizar el total de usuarios para la comunidad ID: {IDCOMUNIDAD}. Error: {ex.Message}.",
                    0, // Ajusta según corresponda
                    DateTime.Now
                );

                Console.WriteLine("Error al actualizar el total de usuarios de la comunidad: " + ex.Message);
                return false;
            }
        }



    }
}
