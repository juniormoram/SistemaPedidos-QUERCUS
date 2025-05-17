using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using QuercusPedidos;

namespace QuercusPedidos.Controllers
{
    public class PedidoDetalleResController : Controller
    {
        private QuercusPedidosEntities db = new QuercusPedidosEntities();

        // GET: PedidoDetalleRes
        public async Task<ActionResult> Index(int buscarD = 0)
        {
            var detalles = from deta in db.PedidoDetalleRes select deta;

            // BUSQUEDA DE LOS DETALLES
            if (buscarD != null)
            {
                detalles = detalles.Where(s => s.Id_ResDetalle.Equals(buscarD));
            }
            return View(await detalles.ToListAsync());
        }

        private void ActualizarTotalesPedido(int idPedido)
        {
            using (QuercusPedidosEntities BD = new QuercusPedidosEntities())
            {
                var pedido = BD.Pedido.Find(idPedido);
                if (pedido == null) return;

                int subTotRes = BD.PedidoDetalleRes
                    .Where(d => d.Id_ResDetalle == pedido.Id_ResDetalle)
                    .Sum(d => (int?)d.CostoTotal) ?? 0;

                int subTotBar = BD.PedidoDetalleBar
                    .Where(d => d.Id_BarDetalle == pedido.Id_BarDetalle)
                    .Sum(d => (int?)d.CostoTotal) ?? 0;

                double iva = 0.13;
                double impServi = 0.10;

                int totalBruto = subTotRes + subTotBar;
                int ivaCalc = (int)(iva * totalBruto);
                int impServCalc = (int)(impServi * totalBruto);
                int totalFinal = totalBruto + impServCalc;

                pedido.Subtotal = totalBruto;
                pedido.Iva = ivaCalc;
                pedido.Total = totalFinal;
                pedido.Fecha = DateTime.Now;

                BD.Entry(pedido).State = EntityState.Modified;
                BD.SaveChanges();
            }
        }

        // GET: PedidoDetalleRes/Create
        public ActionResult Create(int? id)
        {
            ViewBag.Id_ProductoRes = new SelectList(db.ProductoRes, "Id_ProductoRes", "Nombre");
            return View();
        }

        // POST: PedidoDetalleRes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id_ResDetalle,Id_ProductoRes,Cantidad,Observacion,CostoTotal")] PedidoDetalleRes pedidoDetalleRes, int? id)
        {
            if (ModelState.IsValid)
            {
                ProductoRes productoRes = new ProductoRes();

                using (QuercusPedidosEntities BD = new QuercusPedidosEntities())
                {
                    ProductoRes prod = (from p in BD.ProductoRes
                                        where p.Id_ProductoRes == pedidoDetalleRes.Id_ProductoRes
                                        select p).First();
                    productoRes = prod;
                }
                try
                {
                    int precioProd = productoRes.Precio;
                    pedidoDetalleRes.CostoTotal = precioProd * pedidoDetalleRes.Cantidad;
                    pedidoDetalleRes.Id_ResDetalle = (int)id;

                    db.PedidoDetalleRes.Add(pedidoDetalleRes);
                    db.SaveChanges();

                    ActualizarTotalesPedido((int)id);

                    return RedirectToAction("Create");
                }
                catch
                {
                    Response.Write("Por favor acortar la observarción del platillo");
                }
            }

            ViewBag.Id_ProductoRes = new SelectList(db.ProductoRes, "Id_ProductoRes", "Nombre", pedidoDetalleRes.Id_ProductoRes);
            return View(pedidoDetalleRes);
        }

        // GET: PedidoDetalleRes/Edit/5
        public ActionResult Edit(int? id, int? id2)
        {
            if (id == null && id2 == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            PedidoDetalleRes pedidoDetalleRes = db.PedidoDetalleRes.Find(id, id2);
            if (pedidoDetalleRes == null)
            {
                return HttpNotFound();
            }
            ViewBag.Id_ProductoRes = new SelectList(db.ProductoRes, "Id_ProductoRes", "Nombre", pedidoDetalleRes.Id_ProductoRes);
            return View(pedidoDetalleRes);
        }

        // POST: PedidoDetalleRes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id_ResDetalle,Id_ProductoRes,Cantidad,Observacion,CostoTotal")] PedidoDetalleRes pedidoDetalleRes, int? id)
        {
            if (ModelState.IsValid)
            {
                //ProductoRes productoRes = new ProductoRes();

                //using (QuercusPedidosEntities BD = new QuercusPedidosEntities())
                //{
                //    ProductoRes prod = (from p in BD.ProductoRes
                //                        where p.Id_ProductoRes == pedidoDetalleRes.Id_ProductoRes
                //                        select p).First();
                //    productoRes = prod;
                //}

                try
                {
                    //int precioProd = productoRes.Precio;
                    //pedidoDetalleRes.CostoTotal = precioProd * pedidoDetalleRes.Cantidad;
                    pedidoDetalleRes.Id_ResDetalle = (int)id;

                    db.Entry(pedidoDetalleRes).State = EntityState.Modified;
                    db.SaveChanges();

                    ActualizarTotalesPedido((int)id);

                    return RedirectToAction("Index");
                }
                catch
                {
                    Response.Write("Por favor acortar la observarción del platillo");
                }
            }
            ViewBag.Id_ProductoRes = new SelectList(db.ProductoRes, "Id_ProductoRes", "Nombre", pedidoDetalleRes.Id_ProductoRes);
            return View(pedidoDetalleRes);
        }

        // GET: PedidoDetalleRes/Delete/5
        public ActionResult Delete(int? id, int? id2)
        {
            if (id == null && id2 == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            PedidoDetalleRes pedidoDetalleRes = db.PedidoDetalleRes.Find(id, id2);
            if (pedidoDetalleRes == null)
            {
                return HttpNotFound();
            }
            return View(pedidoDetalleRes);
        }

        // POST: PedidoDetalleRes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id, int id2)
        {
            PedidoDetalleRes pedidoDetalleRes = db.PedidoDetalleRes.Find(id, id2);
            db.PedidoDetalleRes.Remove(pedidoDetalleRes);
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
