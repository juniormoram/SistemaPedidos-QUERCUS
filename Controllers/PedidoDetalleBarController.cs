﻿using System;
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
    public class PedidoDetalleBarController : Controller
    {
        private QuercusPedidosEntities db = new QuercusPedidosEntities();

        // GET: PedidoDetalleBar
        public async Task<ActionResult> Index(int buscarD = 0)
        {
            var detalles = from deta in db.PedidoDetalleBar select deta;

            // BUSQUEDA DE LOS DETALLES
            if (buscarD != null)
            {
                detalles = detalles.Where(s => s.Id_BarDetalle.Equals(buscarD));
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

        // GET: PedidoDetalleBar/Create
        public ActionResult Create(int? id)
        {
            ViewBag.Id_ProductoBar = new SelectList(db.ProductoBar, "Id_ProductoBar", "Nombre");
            return View();
        }

        // POST: PedidoDetalleBar/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id_BarDetalle,Id_ProductoBar,Cantidad,CostoTotal")] PedidoDetalleBar pedidoDetalleBar, int? id)
        {
            if (ModelState.IsValid)
            {
                ProductoBar productoBar = new ProductoBar();

                using (QuercusPedidosEntities BD = new QuercusPedidosEntities())
                {
                    ProductoBar prod = (from p in BD.ProductoBar
                                        where p.Id_ProductoBar == pedidoDetalleBar.Id_ProductoBar
                                        select p).First();
                    productoBar = prod;
                }

                int precioProd = productoBar.Precio;
                pedidoDetalleBar.CostoTotal = precioProd * pedidoDetalleBar.Cantidad;
                pedidoDetalleBar.Id_BarDetalle = (int)id;

                db.PedidoDetalleBar.Add(pedidoDetalleBar);
                db.SaveChanges();

                ActualizarTotalesPedido((int)id);

                return RedirectToAction("Create");
            }

            ViewBag.Id_ProductoBar = new SelectList(db.ProductoBar, "Id_ProductoBar", "Nombre", pedidoDetalleBar.Id_ProductoBar);
            return View(pedidoDetalleBar);
        }

        // GET: PedidoDetalleBar/Edit/5
        public ActionResult Edit(int? id, int? id2)
        {
            if (id == null && id2 == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            PedidoDetalleBar pedidoDetalleBar = db.PedidoDetalleBar.Find(id, id2);
            if (pedidoDetalleBar == null)
            {
                return HttpNotFound();
            }
            ViewBag.Id_ProductoBar = new SelectList(db.ProductoBar, "Id_ProductoBar", "Nombre", pedidoDetalleBar.Id_ProductoBar);
            return View(pedidoDetalleBar);
        }

        // POST: PedidoDetalleBar/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id_BarDetalle,Id_ProductoBar,Cantidad,CostoTotal")] PedidoDetalleBar pedidoDetalleBar, int? id)
        {
            if (ModelState.IsValid)
            {
                ProductoBar productoBar = new ProductoBar();

                using (QuercusPedidosEntities BD = new QuercusPedidosEntities())
                {
                    ProductoBar prod = (from p in BD.ProductoBar
                                        where p.Id_ProductoBar == pedidoDetalleBar.Id_ProductoBar
                                        select p).First();
                    productoBar = prod;
                }

                int precioProd = productoBar.Precio;
                pedidoDetalleBar.CostoTotal = precioProd * pedidoDetalleBar.Cantidad;
                pedidoDetalleBar.Id_BarDetalle = (int)id;

                db.Entry(pedidoDetalleBar).State = EntityState.Modified;
                db.SaveChanges();

                ActualizarTotalesPedido((int)id);

                return RedirectToAction("Index");
            }
            ViewBag.Id_ProductoBar = new SelectList(db.ProductoBar, "Id_ProductoBar", "Nombre", pedidoDetalleBar.Id_ProductoBar);
            return View(pedidoDetalleBar);
        }
               
        // GET: PedidoDetalleBar/Delete/5
        public ActionResult Delete(int? id, int? id2)
        {
            if (id == null && id2 == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            PedidoDetalleBar pedidoDetalleBar = db.PedidoDetalleBar.Find(id, id2);
            if (pedidoDetalleBar == null)
            {
                return HttpNotFound();
            }
            return View(pedidoDetalleBar);
        }

        // POST: PedidoDetalleBar/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id, int id2)
        {
            PedidoDetalleBar pedidoDetalleBar = db.PedidoDetalleBar.Find(id, id2);
            db.PedidoDetalleBar.Remove(pedidoDetalleBar);
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
