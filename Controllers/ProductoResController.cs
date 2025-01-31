using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Services.Description;
using System.Web.UI;
using QuercusPedidos;

namespace QuercusPedidos.Controllers
{
    public class ProductoResController : Controller
    {
        private QuercusPedidosEntities db = new QuercusPedidosEntities();

        // GET: ProductoRes
        public async Task<ActionResult> Index(string buscar)
        {
            var productos = from prod in db.ProductoRes
                            where prod.Id_ProductoRes != 1
                            select prod;

            // BUSQUEDA DE LOS PRODUCTOS
            if (!String.IsNullOrEmpty(buscar))
            {
                productos = productos.Where(s => s.Nombre.Contains(buscar));
            }
            return View(await productos.ToListAsync());
        }

        // GET: ProductoRes/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: ProductoRes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id_ProductoRes,Nombre,Precio")] ProductoRes productoRes)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    db.ProductoRes.Add(productoRes);
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                catch(Exception ex)
                {
                    Response.Write("Por favor revisar la información del platillo");                   
                }

            }
            return View(productoRes);
        }

        // GET: ProductoRes/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ProductoRes productoRes = db.ProductoRes.Find(id);
            if (productoRes == null)
            {
                return HttpNotFound();
            }
            return View(productoRes);
        }

        // POST: ProductoRes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id_ProductoRes,Nombre,Precio")] ProductoRes productoRes)
        {
            if (ModelState.IsValid)
            {
                db.Entry(productoRes).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(productoRes);
        }

        // GET: ProductoRes/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ProductoRes productoRes = db.ProductoRes.Find(id);
            if (productoRes == null)
            {
                return HttpNotFound();
            }
            return View(productoRes);
        }

        // POST: ProductoRes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                ProductoRes productoRes = db.ProductoRes.Find(id);
                db.ProductoRes.Remove(productoRes);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                Response.Write("No es posible eliminar el platillo porque se encuentra asociado a un pedido");
            }
            return RedirectToAction("Delete");
            
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
