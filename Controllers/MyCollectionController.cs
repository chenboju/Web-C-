using System;
using System.Linq;
using System.Web.Mvc;
using System.Collections.Generic;
using System.Data.SqlClient;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    // 視圖模型類
    public class CollectionViewModel
    {
        public int CollectionId { get; set; }
        public int PaperId { get; set; }
        public string Title { get; set; }
        public string Authors { get; set; }
        public string Journal { get; set; }
        public int Year { get; set; }
        public DateTime CollectionDate { get; set; }
        public string Notes { get; set; }
    }

    public class MyCollectionController : Controller
    {
        private readonly LiteratureEntities2 db = new LiteratureEntities2();

        // GET: MyCollection
        public ActionResult Index()
        {
            if (Session["UserId"] == null)
            {
                ViewBag.ErrorMessage = "未登入，需要先登入才能訪問收藏功能";
                return View("NotLoggedIn");
            }

            try
            {
                int userId = Convert.ToInt32(Session["UserId"]);
                ViewBag.Debug = "用戶ID：" + userId;

                // 使用明確定義的視圖模型
                var collections = (from c in db.Collection
                                   join p in db.Paper on c.PaperId equals p.PaperId
                                   where c.UserId == userId
                                   orderby c.CollectionDate descending
                                   select new CollectionViewModel
                                   {
                                       CollectionId = c.CollectionId,
                                       PaperId = p.PaperId,
                                       Title = p.Title,
                                       Authors = p.Authors,
                                       Journal = p.Journal,
                                       Year = p.Year,
                                       CollectionDate = c.CollectionDate,
                                       Notes = c.Notes
                                   }).ToList();

                ViewBag.Collections = collections;
                ViewBag.CollectionCount = collections.Count;

                return View();
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "獲取收藏列表時發生錯誤: " + ex.Message;
                ViewBag.ExceptionDetails = ex.ToString();
                ViewBag.Collections = new List<CollectionViewModel>();
                return View();
            }
        }

        // POST: MyCollection/Add
        [HttpPost]
        public ActionResult Add(int? paperId)
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Login", "Home");
            }

            if (paperId == null)
            {
                TempData["ErrorMessage"] = "文獻ID不能為空";
                return RedirectToAction("Papers", "PaperManagement");
            }

            try
            {
                int userId = Convert.ToInt32(Session["UserId"]);

                // 檢查是否已經收藏過這篇文獻
                var existingCollection = db.Collection.FirstOrDefault(c =>
                    c.UserId == userId && c.PaperId == paperId.Value);

                if (existingCollection != null)
                {
                    TempData["Message"] = "此文獻已在您的收藏中";
                    return RedirectToAction("PaperDetail", "PaperManagement", new { id = paperId.Value });
                }

                // 建立新收藏記錄
                var newCollection = new Collection
                {
                    UserId = userId,
                    PaperId = paperId.Value,
                    CollectionDate = DateTime.Now,
                    Notes = ""
                };

                db.Collection.Add(newCollection);
                db.SaveChanges();

                TempData["SuccessMessage"] = "成功添加到收藏";
                return RedirectToAction("PaperDetail", "PaperManagement", new { id = paperId.Value });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "添加收藏時發生錯誤: " + ex.Message;
                return RedirectToAction("PaperDetail", "PaperManagement", new { id = paperId.Value });
            }
        }

        // POST: MyCollection/Remove
        [HttpPost]
        public ActionResult Remove(int? id)
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Login", "Home");
            }

            if (id == null)
            {
                TempData["ErrorMessage"] = "收藏ID不能為空";
                return RedirectToAction("Index");
            }

            try
            {
                int userId = Convert.ToInt32(Session["UserId"]);

                var collection = db.Collection.FirstOrDefault(c =>
                    c.CollectionId == id.Value && c.UserId == userId);

                if (collection == null)
                {
                    TempData["ErrorMessage"] = "找不到指定的收藏項目";
                    return RedirectToAction("Index");
                }

                db.Collection.Remove(collection);
                db.SaveChanges();

                TempData["SuccessMessage"] = "已從收藏中移除";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "移除收藏時發生錯誤: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // POST: MyCollection/UpdateNotes
        [HttpPost]
        public ActionResult UpdateNotes(int? id, string notes)
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Login", "Home");
            }

            if (id == null)
            {
                TempData["ErrorMessage"] = "收藏ID不能為空";
                return RedirectToAction("Index");
            }

            try
            {
                int userId = Convert.ToInt32(Session["UserId"]);

                var collection = db.Collection.FirstOrDefault(c =>
                    c.CollectionId == id.Value && c.UserId == userId);

                if (collection == null)
                {
                    TempData["ErrorMessage"] = "找不到指定的收藏項目";
                    return RedirectToAction("Index");
                }

                collection.Notes = notes;
                db.SaveChanges();

                TempData["SuccessMessage"] = "筆記已更新";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "更新筆記時發生錯誤: " + ex.Message;
                return RedirectToAction("Index");
            }
        }
    }
}