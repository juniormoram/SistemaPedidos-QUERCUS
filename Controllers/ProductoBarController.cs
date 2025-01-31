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
    public class ProductoBarController : Controller
    {
        private QuercusPedidosEntities db = new QuercusPedidosEntities();

        // GET: ProductoBar
        public async Task<ActionResult> Index(string buscar)
        {
            var productos = from prod in db.ProductoBar
                            where prod.Id_ProductoBar != 1
                            select prod;

            // BUSQUEDA DE LOS PRODUCTOS
            if (!String.IsNullOrEmpty(buscar))
            {
                productos = productos.Where(s => s.Nombre.Contains(buscar));
            }
            return View(await productos.ToListAsync());
        }

        // GET: ProductoBar/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: ProductoBar/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id_ProductoBar,Nombre,Precio")] ProductoBar productoBar)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    db.ProductoBar.Add(productoBar);
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    Response.Write("Por favor revisar la información de la bebida");
                }
            }

            return View(productoBar);
        }

        // GET: ProductoBar/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ProductoBar productoBar = db.ProductoBar.Find(id);
            if (productoBar == null)
            {
                return HttpNotFound();
            }
            return View(productoBar);
        }

        // POST: ProductoBar/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id_ProductoBar,Nombre,Precio")] ProductoBar productoBar)
        {
            if (ModelState.IsValid)
            {
                db.Entry(productoBar).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(productoBar);
        }

        // GET: ProductoBar/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ProductoBar productoBar = db.ProductoBar.Find(id);
            if (productoBar == null)
            {
                return HttpNotFound();
            }
            return View(productoBar);
        }

        // POST: ProductoBar/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                ProductoBar productoBar = db.ProductoBar.Find(id);
                db.ProductoBar.Remove(productoBar);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            catch
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
