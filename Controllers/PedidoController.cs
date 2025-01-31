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
            var pedidos = from pedi in db.Pedido select pedi;

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
                                        Nombre = pr.Nombre,
                                        Cantidad = b.Cantidad,
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
                                        Nombre = pr.Nombre,
                                        Cantidad = b.Cantidad,
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
                                    }).ToList();

            var viewModel = new DetallePedido
            {
                DatosBar = detallePedidoBar,
                DatosRes = detallePedidoRes,
                DatosPed = detallePedido,
            };

            return viewModel;
        }

        // Impresion de tiquete
        
        //public ActionResult ImprimirTiquete(int? id)
        //{
        //    DetallePedido detallePedido = ObtenerDetalles(id);

        //    if (detallePedido != null)
        //    {
        //        string contenidoTiquete = GenerarContenidoTiquete(detallePedido);
        //        ImprimirDesdeAplicacionEscritorio(contenidoTiquete);
        //    }
        //    else
        //    {
        //        Console.WriteLine("No se encontraron datos para imprimir el tiquete.");
        //    }

        //    return RedirectToAction("Index", "Pedido");
        //}

        //private void ImprimirDesdeAplicacionEscritorio(string contenidoTiquete)
        //{
        //    try
        //    {
        //        // Ruta de la aplicación de escritorio
        //        string rutaAplicacionEscritorio = @"C:\Users\DELL\Desktop\EjecutablePrinterApp\PrinterApp";

        //        // Ejecutar la aplicación de escritorio con los datos de impresión como argumentos
        //        Process.Start(rutaAplicacionEscritorio, contenidoTiquete);
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine("Error al invocar la aplicación de escritorio: " + ex.Message);
        //    }
        //}

        //private DetallePedido ObtenerDetalles(int? id)
        //{
        //    var detallePedidoBar = (from p in db.Pedido
        //                            join b in db.PedidoDetalleBar on p.Id_Pedido equals b.Id_BarDetalle
        //                            join pr in db.ProductoBar on b.Id_ProductoBar equals pr.Id_ProductoBar
        //                            where b.Id_BarDetalle == id
        //                            select new DetallePedido
        //                            {
        //                                IdPedido = p.Id_Pedido,
        //                                Nombre = pr.Nombre,
        //                                Cantidad = b.Cantidad,
        //                                CostoTotal = b.CostoTotal,
        //                                MontoTotal = p.Total,
        //                            }).ToList();

        //    var detallePedidoRes = (from p in db.Pedido
        //                            join b in db.PedidoDetalleRes on p.Id_Pedido equals b.Id_ResDetalle
        //                            join pr in db.ProductoRes on b.Id_ProductoRes equals pr.Id_ProductoRes
        //                            where b.Id_ResDetalle == id
        //                            select new DetallePedido
        //                            {
        //                                IdPedido = p.Id_Pedido,
        //                                Nombre = pr.Nombre,
        //                                Cantidad = b.Cantidad,
        //                                CostoTotal = b.CostoTotal,
        //                                Observacion = b.Observacion,
        //                            }).ToList();

        //    var detallePedidos = (from p in db.Pedido
        //                         //join b in db.PedidoDetalleRes on p.Id_Pedido equals b.Id_ResDetalle
        //                         where p.Id_Pedido == id
        //                         select new DetallePedido
        //                         {
        //                             IdPedido = p.Id_Pedido,
        //                             Mesa = p.Mesa,
        //                             MontoTotal = p.Total,
        //                         }).ToList();

        //    var detallles = new DetallePedido
        //    {
        //        DatosBar = detallePedidoBar,
        //        DatosRes = detallePedidoRes,
        //        DatosPed = detallePedidos,
        //    };

        //    return detallles;
        //}

        //private string GenerarContenidoTiquete(DetallePedido detallePedido)
        //{
        //    // Genera el contenido del tiquete con el formato adecuado
        //    StringBuilder contenidoTiquete = new StringBuilder();

        //    // Encabezado del tiquete
        //    contenidoTiquete.AppendLine("QUERCUS RES - Tel: 2201-5082");
        //    contenidoTiquete.AppendLine($"Mesa/Cliente: {detallePedido.Mesa}");

        //    // Detalles del pedido (por ejemplo, datos del bar)
        //    contenidoTiquete.AppendLine("Bebidas");
        //    foreach (var datoBar in detallePedido.DatosBar)
        //    {
        //        contenidoTiquete.AppendLine($"Producto: {datoBar.Nombre}");
        //        contenidoTiquete.AppendLine($"Cantidad: {datoBar.Cantidad}");
        //        contenidoTiquete.AppendLine($"Costo Total: {datoBar.CostoTotal}");
        //    }
        //    // Detalles del pedido (por ejemplo, datos del restaurante)
        //    contenidoTiquete.AppendLine("Platillos");
        //    foreach (var datoRes in detallePedido.DatosRes)
        //    {
        //        contenidoTiquete.AppendLine($"Producto: {datoRes.Nombre}");
        //        contenidoTiquete.AppendLine($"Cantidad: {datoRes.Cantidad}");
        //        contenidoTiquete.AppendLine($"Costo Total: {datoRes.CostoTotal}");                
        //    }
        //    contenidoTiquete.AppendLine($"Monto Total: {detallePedido.MontoTotal}");
        //    // Agregar más información del tiquete según sea necesario

        //    return contenidoTiquete.ToString();
        //}
        //private void ImprimirTiquete(string contenidoTiquete)
        //{
        //    string puertoUSB = @"USB001"; // Puerto USB donde está conectada la impresora

        //    try
        //    {
        //        // Abrir el puerto USB donde está conectada la impresora
        //        using (FileStream fs = new FileStream(@"\\.\" + puertoUSB, FileMode.Open))
        //        {
        //            // Crear un flujo de escritura para enviar los datos al puerto USB
        //            using (StreamWriter sw = new StreamWriter(fs))
        //            {
        //                // Inicializar la impresora
        //                sw.Write("\x1B@");

        //                // Establecer alineación izquierda
        //                sw.Write("\x1Ba\x00");

        //                // Imprimir el contenido del tiquete
        //                sw.Write(contenidoTiquete);

        //                // Cortar el papel
        //                sw.Write("\x1DVA\x00");

        //                // Finalizar la transmisión y cerrar los recursos
        //                sw.Flush();
        //            }
        //        }

        //        Console.WriteLine("Tiquete impreso correctamente.");
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine("Error al imprimir el tiquete: " + ex.Message);
        //    }
        //}

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
        public ActionResult Create([Bind(Include = "Id_Pedido,Mesa,Estado,Fecha,Subtotal,Iva,Total,Id_ResDetalle,Id_ProductoRes,Id_BarDetalle,Id_ProductoBar")] Pedido pedido)
        {
            pedido.Id_BarDetalle = pedido.Id_Pedido;
            pedido.Id_ResDetalle = pedido.Id_Pedido;

            if (ModelState.IsValid)
            {
                PedidoDetalleRes DetalleRes = new PedidoDetalleRes();
                PedidoDetalleBar DetalleBar = new PedidoDetalleBar();
                double iva = 0.13;
                int IVA = 0;

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
                                
                IVA = (int)(iva * (subTotRes + subTotBar));
                pedido.Fecha = DateTime.Now;
                pedido.Subtotal = subTotRes + subTotBar - IVA;
                pedido.Iva = (int)(iva * (subTotRes + subTotBar));
                pedido.Total = subTotRes + subTotBar;
                
                db.Pedido.Add(pedido);
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
        public ActionResult Edit([Bind(Include = "Id_Pedido,Mesa,Estado,Fecha,Subtotal,Iva,Total,Id_ResDetalle,Id_ProductoRes,Id_BarDetalle,Id_ProductoBar")] Pedido pedido)
        {
            pedido.Id_BarDetalle = pedido.Id_Pedido;
            pedido.Id_ResDetalle = pedido.Id_Pedido;

            if (ModelState.IsValid)
            {
                PedidoDetalleRes DetalleRes = new PedidoDetalleRes();
                PedidoDetalleBar DetalleBar = new PedidoDetalleBar();
                double iva = 0.13;
                int IVA = 0;

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
                                
                IVA = (int)(iva * (subTotRes + subTotBar));                
                pedido.Subtotal = subTotRes + subTotBar - IVA;
                pedido.Iva = (int)(iva * (subTotRes + subTotBar));
                pedido.Total = subTotRes + subTotBar;
                                
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
