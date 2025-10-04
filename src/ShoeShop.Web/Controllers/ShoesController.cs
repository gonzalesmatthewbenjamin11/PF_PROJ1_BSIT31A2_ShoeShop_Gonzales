using Microsoft.AspNetCore.Mvc;
using ShoeShop.Infrastructure;
using ShoeShop.Infrastructure.Entities;

namespace ShoeShop.Web.Controllers
{
    public class ShoesController : Controller
    {
        private readonly ShoeShopContext _context;

        public ShoesController(ShoeShopContext context)
        {
            _context = context;
        }

        // GET: /Shoes
        public IActionResult Index()
        {
            var shoes = _context.Shoes.ToList();
            return View(shoes);
        }

        // GET: /Shoes/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Shoes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Shoe shoe)
        {
            if (ModelState.IsValid)
            {
                _context.Shoes.Add(shoe);
                _context.SaveChanges();
                TempData["Success"] = "Shoe added successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(shoe);
        }

        // GET: /Shoes/Edit/5
        public IActionResult Edit(int id)
        {
            var shoe = _context.Shoes.Find(id);
            if (shoe == null)
            {
                return NotFound();
            }
            return View(shoe);
        }

        // POST: /Shoes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Shoe shoe)
        {
            if (id != shoe.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                _context.Update(shoe);
                _context.SaveChanges();
                TempData["Success"] = "Shoe updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(shoe);
        }

        // GET: /Shoes/Details/5
        public IActionResult Details(int id)
        {
            var shoe = _context.Shoes.Find(id);
            if (shoe == null)
            {
                return NotFound();
            }
            return View(shoe);
        }

        // GET: /Shoes/Delete/5
        public IActionResult Delete(int id)
        {
            var shoe = _context.Shoes.Find(id);
            if (shoe == null)
            {
                return NotFound();
            }
            return View(shoe);
        }

        // POST: /Shoes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var shoe = _context.Shoes.Find(id);
            if (shoe != null)
            {
                _context.Shoes.Remove(shoe);
                _context.SaveChanges();
                TempData["Success"] = "Shoe deleted successfully!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
