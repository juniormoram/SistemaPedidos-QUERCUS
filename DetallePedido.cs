using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading.Tasks;
using System.Data.Entity;
using System.Web.UI.WebControls;

namespace QuercusPedidos
{
    public class DetallePedido
    {
        public List<DetallePedido> DatosBar { get; set; }
        public List<DetallePedido> DatosRes { get; set; }
        public List<DetallePedido> DatosPed { get; set; }
        public List<DetallePedido> DatosCierre { get; set; }
        public List<DetallePedido> DatosMoneda { get; set; }
        public int IdPedido { get; set; }
        public int Cantidad { get; set; }
        public string Nombre { get; set; }
        public string Mesa { get; set; }
        public string Observacion { get; set; }
        public int CostoTotal { get; set; }
        public double ImpServicio { get; set; }
        public int MontoTotalB { get; set; }
        public int MontoTotalR { get; set; }
        public int MontoTotal { get; set; }
        public int SubTotal { get; set; }
        public int Monto { get; set; }
        public DateTime Fecha { get; set; }
        public int MontoRes { get; set; }
        public int MontoBar { get; set; }
    }
}