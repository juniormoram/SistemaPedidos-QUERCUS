using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Entity;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web.UI;
using RawPrint;

namespace QuercusPedidos.Controllers
{
    public class PedidoController : Controller
    {
        private QuercusPedidosEntities db = new QuercusPedidosEntities();

        // GET: Pedido
        public async Task<ActionResult> Index(string buscar, string filtro)
        {
            var pedidos = from pedi in db.Pedido 
                          where DbFunctions.TruncateTime(pedi.Fecha) == DbFunctions.TruncateTime(DateTime.Now)
                          select pedi;

            // BUSQUEDA DE LOS PEDIDOS
            if (!String.IsNullOrEmpty(buscar))
            {
                pedidos = pedidos.Where(s => s.Id_Pedido.Equals(buscar));
            }

            // ORDENACION DE PEDIDOS
            ViewData["FiltroPedido"] = String.IsNullOrEmpty(filtro) ? "PedidoDescendente" : "";

            switch (filtro)
            {
                case "PedidoDescendente":
                    pedidos = pedidos.OrderBy(pedi => pedi.Id_Pedido);
                    break;
                default:
                    pedidos = pedidos.OrderByDescending(pedi => pedi.Id_Pedido);
                    break;
            }
            return View(await pedidos.ToListAsync());
        }

        public ActionResult Cierre(DateTime? fechaCierre)
        {
            if (fechaCierre == null)
            {
                fechaCierre = DateTime.Now;
            }
            else
            {
                fechaCierre = fechaCierre;
            };

            DetallePedido viewModel = CierreCaja(fechaCierre);
            return View(viewModel);
        }               

        //Consultas para obtener cierre de caja
        public DetallePedido CierreCaja(DateTime? fechaCierre)
        {
            int cierreTotal = 0;
            int cierreBar = 0;
            int cierreRes = 0;
            DateTime fecha = fechaCierre.Value;

            var detalleCierre = (from p in db.Pedido
                                 where p.Estado == true && DbFunctions.TruncateTime(p.Fecha) == DbFunctions.TruncateTime(fecha)
                                 select new DetallePedido
                                 {
                                     IdPedido = p.Id_Pedido,
                                     Mesa = p.Mesa,
                                     Fecha = p.Fecha,
                                     Monto = p.Total,
                                 }).ToList();

            var detalleCuentaBar = (from p in db.Pedido
                                 where p.Estado == true && DbFunctions.TruncateTime(p.Fecha) == DbFunctions.TruncateTime(fecha)
                                 join b in db.PedidoDetalleBar on p.Id_Pedido equals b.Id_BarDetalle
                                 select new DetallePedido
                                 {                                        
                                     MontoTotalB = b.CostoTotal,
                                 }).ToList();

            var detalleCuentaRes = (from p in db.Pedido
                                    where p.Estado == true && DbFunctions.TruncateTime(p.Fecha) == DbFunctions.TruncateTime(fecha)
                                    join c in db.PedidoDetalleRes on p.Id_Pedido equals c.Id_ResDetalle
                                    select new DetallePedido
                                    {
                                        MontoTotalR = c.CostoTotal,
                                    }).ToList();

            List<DetallePedido> ListMontos = new List<DetallePedido>();
            List<DetallePedido> DetalleCierre = new List<DetallePedido>();

            foreach (var d in detalleCierre)
            {
                cierreTotal += d.Monto;
            }

            foreach (var d in detalleCuentaBar)
            {
                cierreBar += d.MontoTotalB;
            }

            foreach (var d in detalleCuentaRes)
            {
                cierreRes += d.MontoTotalR;
            }

            ListMontos.Add(new DetallePedido
            {
                MontoTotal = cierreTotal,
                MontoTotalB = cierreBar,
                MontoTotalR = cierreRes,
            });

            var viewModel = new DetallePedido
            {
                DatosCierre = detalleCierre,
                DatosMoneda = ListMontos
            };

            return viewModel;
        }

        // GET: Pedido/Detalles
        public ActionResult DetallePedido(int? id)
        {            
            DetallePedido viewModel = ObtenerDetalle(id);
            return View(viewModel);
        }

        public ActionResult imprimir(int? id)
        {
            DetallePedido viewModel = ObtenerDetalle(id);
            return View(viewModel);
        }

        [HttpPost]
        public ActionResult ActualizarEstadoPedido(int id)
        {
            try
            {
                var pedido = db.Pedido.FirstOrDefault(p => p.Id_Pedido == id);
                if (pedido != null)
                {
                    pedido.Estado = true;
                    db.SaveChanges();
                    return Json(new { success = true });
                }

                return Json(new { success = false, message = $"Pedido con ID {id} no encontrado." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error interno: " + ex.Message });
            }
        }

        //Consultas para obtener bebidas y platillos del pedido
        public DetallePedido ObtenerDetalle(int? id)
        {            
            var detallePedidoBar = (from p in db.Pedido
                                    join b in db.PedidoDetalleBar on p.Id_Pedido equals b.Id_BarDetalle
                                    join pr in db.ProductoBar on b.Id_ProductoBar equals pr.Id_ProductoBar
                                    where b.Id_BarDetalle == id
                                    select new DetallePedido 
                                    {
                                        IdPedido = p.Id_Pedido,
                                        IdDetalleBar = b.Id_BarDetalle,
                                        IdProductoBar = pr.Id_ProductoBar,
                                        Nombre = pr.Nombre,
                                        Cantidad = b.Cantidad,
                                        PrecioUnitario = pr.Precio,
                                        CostoTotal = b.CostoTotal,
                                        MontoTotal = p.Total,
                                    }).ToList();

            var detallePedidoRes = (from p in db.Pedido
                                    join b in db.PedidoDetalleRes on p.Id_Pedido equals b.Id_ResDetalle
                                    join pr in db.ProductoRes on b.Id_ProductoRes equals pr.Id_ProductoRes
                                    where b.Id_ResDetalle == id
                                    select new DetallePedido
                                    {
                                        IdPedido = p.Id_Pedido,
                                        IdDetalleRes = b.Id_ResDetalle,
                                        IdProductoRes = pr.Id_ProductoRes,
                                        Nombre = pr.Nombre,
                                        Cantidad = b.Cantidad,
                                        PrecioUnitario = pr.Precio,
                                        CostoTotal = b.CostoTotal,
                                        Observacion = b.Observacion,
                                    }).ToList();

            var detallePedido =     (from p in db.Pedido
                                    //join b in db.PedidoDetalleRes on p.Id_Pedido equals b.Id_ResDetalle
                                    where p.Id_Pedido == id
                                    select new DetallePedido
                                    {
                                        IdPedido = p.Id_Pedido,
                                        Mesa = p.Mesa,
                                        MontoTotal = p.Total,
                                        ImpServicio = p.RequiereServicio ? p.Subtotal * 0.10 : 0,
                                        SubTotal = p.Subtotal,
                                    }).ToList();

            var viewModel = new DetallePedido
            {
                DatosBar = detallePedidoBar,
                DatosRes = detallePedidoRes,
                DatosPed = detallePedido,
            };

            return viewModel;
        }

        //Actualizar cantidades de bebidas y platillos en pantalla "Detalles"
        [HttpPost]
        public ActionResult ActualizarCantidad(string tipo, int idDetalle, int idProducto, int nuevaCantidad)
        {
            try
            {
                int impServicio = 0;
                double impServi = 0.10;
                double iva = 0.13;

                if (tipo == "bar")
                {                    
                    var fila = db.PedidoDetalleBar
                                 .FirstOrDefault(b => b.Id_BarDetalle == idDetalle
                                                   && b.Id_ProductoBar == idProducto);
                    if (fila == null)
                        return Json(new { success = false, msg = "Bebida no encontrada" });

                    var producto = db.ProductoBar.Find(idProducto);
                    fila.Cantidad = nuevaCantidad;
                    fila.CostoTotal = nuevaCantidad * producto.Precio;
                    db.SaveChanges();
                }
                else // res
                {                    
                    var fila = db.PedidoDetalleRes
                                 .FirstOrDefault(r => r.Id_ResDetalle == idDetalle
                                                   && r.Id_ProductoRes == idProducto);
                    if (fila == null)
                        return Json(new { success = false, msg = "Platillo no encontrado" });

                    var producto = db.ProductoRes.Find(idProducto);
                    fila.Cantidad = nuevaCantidad;
                    fila.CostoTotal = nuevaCantidad * producto.Precio;
                    db.SaveChanges();
                }

                // Recalcular totales del pedido
                int subTotBar = db.PedidoDetalleBar
                                  .Where(b => b.Id_BarDetalle == idDetalle)
                                  .Sum(b => (int?)b.CostoTotal) ?? 0;

                int subTotRes = db.PedidoDetalleRes
                                  .Where(r => r.Id_ResDetalle == idDetalle)
                                  .Sum(r => (int?)r.CostoTotal) ?? 0;

                var pedido = db.Pedido.Find(idDetalle); 

                int subtotal = subTotBar + subTotRes;

                if (pedido.RequiereServicio == true)
                {
                    impServicio = (int)(impServi * subtotal);
                }
                else
                {
                    impServicio = 0;
                }
                
                int ivaCalc = (int)(iva * subtotal);
                int total = subtotal + impServicio;
                
                pedido.Subtotal = subtotal;
                pedido.Iva = ivaCalc;
                pedido.Total = total;
                db.SaveChanges();

                return Json(new { success = true, subtotal, impServicio, total });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, msg = ex.Message });
            }
        }

        // GET: Pedido/Create
        public ActionResult Create()
        {
            ViewBag.Id_ResDetalle = new SelectList(db.PedidoDetalleRes, "Id_ResDetalle", "Observacion");
            ViewBag.Id_BarDetalle = new SelectList(db.PedidoDetalleBar, "Id_BarDetalle", "Id_BarDetalle");
            return View();
        }

        // POST: Pedido/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id_Pedido,Mesa,Estado,Fecha,Subtotal,Iva,Total,Id_ResDetalle,Id_ProductoRes,Id_BarDetalle,Id_ProductoBar, RequiereServicio")] Pedido pedido)
        {            
            if (ModelState.IsValid)
            {
                PedidoDetalleRes DetalleRes = new PedidoDetalleRes();
                PedidoDetalleBar DetalleBar = new PedidoDetalleBar();
                double impServi = 0.10;
                double iva = 0.13;
                int ivaCal = 0;
                int impServiCal = 0;

                List<PedidoDetalleRes> ListDetalleRes = new List<PedidoDetalleRes>();
                int subTotRes = 0;

                List<PedidoDetalleBar> ListDetalleBar = new List<PedidoDetalleBar>();
                int subTotBar = 0;

                using (QuercusPedidosEntities BD = new QuercusPedidosEntities())
                {
                    var query = (from d in BD.PedidoDetalleRes
                                 where d.Id_ResDetalle == pedido.Id_ResDetalle
                                 select d);
                    foreach (var d in query)
                    {
                        ListDetalleRes.Add(new PedidoDetalleRes
                        {
                            CostoTotal = d.CostoTotal
                        });
                        subTotRes += d.CostoTotal;
                    }
                }

                using (QuercusPedidosEntities BD = new QuercusPedidosEntities())
                {
                    var query = (from d in BD.PedidoDetalleBar
                                 where d.Id_BarDetalle == pedido.Id_BarDetalle
                                 select d);
                    foreach (var d in query)
                    {
                        ListDetalleBar.Add(new PedidoDetalleBar
                        {
                            CostoTotal = d.CostoTotal
                        });
                        subTotBar += d.CostoTotal;
                    }
                }
                                                               
                ivaCal = (int)(iva * (subTotRes + subTotBar));

                if (pedido.RequiereServicio == true)
                {
                    impServiCal = (int)(impServi * (subTotRes + subTotBar));
                } else
                {
                    impServiCal = 0;
                }

                pedido.Fecha = DateTime.Now;
                pedido.Subtotal = subTotRes + subTotBar;
                pedido.Iva = ivaCal;                
                pedido.Total = subTotRes + subTotBar + impServiCal;
                
                db.Pedido.Add(pedido);
                db.SaveChanges();

                pedido.Id_BarDetalle = pedido.Id_Pedido;
                pedido.Id_ResDetalle = pedido.Id_Pedido;

                db.Entry(pedido).State = EntityState.Modified;
                db.SaveChanges();

                return RedirectToAction("Index");

            }
            
            ViewBag.Id_ResDetalle = new SelectList(db.PedidoDetalleRes, "Id_ResDetalle", "Observacion", pedido.Id_ResDetalle);
            ViewBag.Id_BarDetalle = new SelectList(db.PedidoDetalleBar, "Id_BarDetalle", "Id_BarDetalle", pedido.Id_BarDetalle);
            return View(pedido);
            
        }

        // GET: Pedido/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Pedido pedido = db.Pedido.Find(id);
            if (pedido == null)
            {
                return HttpNotFound();
            }
            ViewBag.Id_ResDetalle = new SelectList(db.PedidoDetalleRes, "Id_ResDetalle", "Id_ResDetalle", pedido.Id_ResDetalle);
            ViewBag.Id_BarDetalle = new SelectList(db.PedidoDetalleBar, "Id_BarDetalle", "Id_BarDetalle", pedido.Id_BarDetalle);
            return View(pedido);
        }

        // POST: Pedido/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id_Pedido,Mesa,Estado,Fecha,Subtotal,Iva,Total,Id_ResDetalle,Id_ProductoRes,Id_BarDetalle,Id_ProductoBar, RequiereServicio")] Pedido pedido)
        {
            pedido.Id_BarDetalle = pedido.Id_Pedido;
            pedido.Id_ResDetalle = pedido.Id_Pedido;

            if (ModelState.IsValid)
            {
                PedidoDetalleRes DetalleRes = new PedidoDetalleRes();
                PedidoDetalleBar DetalleBar = new PedidoDetalleBar();
                double impServi = 0.10;
                double iva = 0.13;
                int ivaCal = 0;
                int impServiCal = 0;

                List<PedidoDetalleRes> ListDetalleRes = new List<PedidoDetalleRes>();
                int subTotRes = 0;

                List<PedidoDetalleBar> ListDetalleBar = new List<PedidoDetalleBar>();
                int subTotBar = 0;

                using (QuercusPedidosEntities BD = new QuercusPedidosEntities())
                {
                    var query = (from d in BD.PedidoDetalleRes
                                 where d.Id_ResDetalle == pedido.Id_ResDetalle
                                 select d);
                    foreach (var d in query)
                    {
                        ListDetalleRes.Add(new PedidoDetalleRes
                        {
                            CostoTotal = d.CostoTotal
                        });
                        subTotRes += d.CostoTotal;
                    }
                }

                using (QuercusPedidosEntities BD = new QuercusPedidosEntities())
                {
                    var query = (from d in BD.PedidoDetalleBar
                                 where d.Id_BarDetalle == pedido.Id_BarDetalle
                                 select d);
                    foreach (var d in query)
                    {
                        ListDetalleBar.Add(new PedidoDetalleBar
                        {
                            CostoTotal = d.CostoTotal
                        });
                        subTotBar += d.CostoTotal;
                    }
                }

                ivaCal = (int)(iva * (subTotRes + subTotBar));

                if (pedido.RequiereServicio == true)
                {
                    impServiCal = (int)(impServi * (subTotRes + subTotBar));
                }
                else
                {
                    impServiCal = 0;
                }
                
                pedido.Fecha = DateTime.Now;
                pedido.Subtotal = subTotRes + subTotBar;
                pedido.Iva = ivaCal;
                pedido.Total = subTotRes + subTotBar + impServiCal;

                db.Entry(pedido).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.Id_ResDetalle = new SelectList(db.PedidoDetalleRes, "Id_ResDetalle", "Observacion", pedido.Id_ResDetalle);
            ViewBag.Id_BarDetalle = new SelectList(db.PedidoDetalleBar, "Id_BarDetalle", "Id_BarDetalle", pedido.Id_BarDetalle);
            return View(pedido);
        }

        // GET: Pedido/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Pedido pedido = db.Pedido.Find(id);
            if (pedido == null)
            {
                return HttpNotFound();
            }
            return View(pedido);
        }

        // POST: Pedido/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Pedido pedido = db.Pedido.Find(id);
            db.Pedido.Remove(pedido);
            db.SaveChanges();
            return RedirectToAction("Index");
        }       

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }

}
