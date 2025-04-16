using System;
using System.Linq;
using System.Web.Mvc;
using System.Collections.Generic;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class PaperManagementController : Controller
    {
        private readonly LiteratureEntities2 db = new LiteratureEntities2();

        // 文獻列表頁面
        public ActionResult Papers(string keyword, string sort, int page = 1)
        {
            try
            {
                // 設置每頁顯示的文獻數
                int pageSize = 10;

                // 查詢所有活躍的文獻
                var query = db.Paper.Where(p => p.IsActive);

                // 如果有關鍵字，進行搜尋
                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    string lowerKeyword = keyword.ToLower();
                    query = query.Where(p =>
                        p.Title.ToLower().Contains(lowerKeyword) ||
                        p.Authors.ToLower().Contains(lowerKeyword) ||
                        p.Abstract.ToLower().Contains(lowerKeyword) ||
                        (p.Keywords != null && p.Keywords.ToLower().Contains(lowerKeyword)));
                }

                // 根據排序選項排序
                switch (sort)
                {
                    case "clickCount":
                        query = query.OrderByDescending(p => p.ClickCount);
                        break;
                    case "title":
                        query = query.OrderBy(p => p.Title);
                        break;
                    case "journal":
                        query = query.OrderBy(p => p.Journal);
                        break;
                    case "year":
                    default:
                        query = query.OrderByDescending(p => p.Year);
                        break;
                }

                // 計算總項目數和總頁數
                int totalItems = query.Count();
                int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                // 處理頁碼邊界
                page = Math.Max(1, Math.Min(page, totalPages > 0 ? totalPages : 1));

                // 取得當前頁的資料
                var papers = query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // 計算每篇文獻的讚數
                var paperIds = papers.Select(p => p.PaperId).ToList();
                var likeCounts = db.Likes
                    .Where(l => paperIds.Contains(l.PaperId))
                    .GroupBy(l => l.PaperId)
                    .ToDictionary(g => g.Key, g => g.Count());

                ViewBag.LikeCounts = likeCounts;

                // 將資料傳遞給視圖
                ViewBag.Papers = papers;
                ViewBag.TotalPages = totalPages;
                ViewBag.CurrentPage = page;

                return View();
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "載入文獻列表時發生錯誤: " + ex.Message;
                ViewBag.Papers = new List<object>();
                return View();
            }
        }

        // 添加讚
        [HttpPost]
        public ActionResult AddLike(int? paperId)
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Login", "Home");
            }

            if (paperId == null)
            {
                TempData["ErrorMessage"] = "文獻ID不能為空";
                return RedirectToAction("Papers");
            }

            try
            {
                int userId = Convert.ToInt32(Session["UserId"]);

                // 檢查用戶是否已經讚過這篇文獻
                var existingLike = db.Likes.FirstOrDefault(l =>
                    l.UserId == userId && l.PaperId == paperId.Value);

                if (existingLike != null)
                {
                    TempData["Message"] = "您已經讚過這篇文獻";
                    return RedirectToAction("PaperDetail", new { id = paperId.Value });
                }

                // 新增讚記錄
                var newLike = new Likes
                {
                    UserId = userId,
                    PaperId = paperId.Value,
                    LikeDate = DateTime.Now
                };

                db.Likes.Add(newLike);
                db.SaveChanges();

                TempData["SuccessMessage"] = "成功讚了這篇文獻";
                return RedirectToAction("PaperDetail", new { id = paperId.Value });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "添加讚時發生錯誤: " + ex.Message;
                return RedirectToAction("PaperDetail", new { id = paperId.Value });
            }
        }

        // 取消讚
        [HttpPost]
        public ActionResult RemoveLike(int? paperId)
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Login", "Home");
            }

            if (paperId == null)
            {
                TempData["ErrorMessage"] = "文獻ID不能為空";
                return RedirectToAction("Papers");
            }

            try
            {
                int userId = Convert.ToInt32(Session["UserId"]);

                // 查找用戶對這篇文獻的讚記錄
                var like = db.Likes.FirstOrDefault(l =>
                    l.UserId == userId && l.PaperId == paperId.Value);

                if (like == null)
                {
                    TempData["Message"] = "您沒有讚過這篇文獻";
                    return RedirectToAction("PaperDetail", new { id = paperId.Value });
                }

                // 移除讚記錄
                db.Likes.Remove(like);
                db.SaveChanges();

                TempData["SuccessMessage"] = "已取消對這篇文獻的讚";
                return RedirectToAction("PaperDetail", new { id = paperId.Value });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "取消讚時發生錯誤: " + ex.Message;
                return RedirectToAction("PaperDetail", new { id = paperId.Value });
            }
        }

        // 文獻詳情頁面
        public ActionResult PaperDetail(int? id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "文獻ID不能為空";
                return RedirectToAction("Papers");
            }

            try
            {
                // 查詢指定ID的文獻
                var paper = db.Paper.FirstOrDefault(p => p.PaperId == id.Value);

                if (paper == null)
                {
                    TempData["ErrorMessage"] = "找不到指定的文獻";
                    return RedirectToAction("Papers");
                }

                // 增加點擊數
                paper.ClickCount++;
                db.SaveChanges();

                // 將文獻資料傳遞給視圖
                ViewBag.Paper = paper;

                // 查詢相關文獻 (根據同一作者或相同關鍵字)
                var relatedPapers = new List<object>();
                if (!string.IsNullOrEmpty(paper.Keywords))
                {
                    // 根據關鍵字查詢相關文獻
                    var keywords = paper.Keywords.Split(',').Select(k => k.Trim()).ToList();

                    relatedPapers = db.Paper
                        .Where(p => p.PaperId != id.Value && p.IsActive &&
                                    (keywords.Any(k => p.Keywords != null && p.Keywords.Contains(k)) ||
                                     p.Authors.Contains(paper.Authors)))
                        .OrderByDescending(p => p.Year)
                        .Take(5)
                        .ToList<object>();
                }

                ViewBag.RelatedPapers = relatedPapers;

                // 檢查當前用戶是否已收藏該文獻
                if (Session["UserId"] != null)
                {
                    int userId = Convert.ToInt32(Session["UserId"]);
                    bool isCollected = db.Collection.Any(c => c.UserId == userId && c.PaperId == id.Value);
                    ViewBag.IsCollected = isCollected;

                    // 檢查當前用戶是否已讚該文獻
                    bool isLiked = db.Likes.Any(l => l.UserId == userId && l.PaperId == id.Value);
                    ViewBag.IsLiked = isLiked;
                }
                else
                {
                    ViewBag.IsCollected = false;
                    ViewBag.IsLiked = false;
                }

                // 獲取該文獻的總讚數
                int likeCount = db.Likes.Count(l => l.PaperId == id.Value);
                ViewBag.LikeCount = likeCount;

                return View();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "載入文獻詳情時發生錯誤: " + ex.Message;
                return RedirectToAction("Papers");
            }
        }

        // 搜尋文獻
        public ActionResult Search(string keyword)
        {
            ViewBag.Keyword = keyword;

            if (string.IsNullOrWhiteSpace(keyword))
            {
                ViewBag.SearchResults = new List<object>();
                ViewBag.ResultCount = 0;
                return View();
            }

            try
            {
                // 將搜尋關鍵字轉為小寫進行不區分大小寫的搜尋
                string lowerKeyword = keyword.ToLower();

                // 從資料庫中搜尋符合關鍵字的文獻
                var results = db.Paper
                    .Where(p =>
                        p.Title.ToLower().Contains(lowerKeyword) ||
                        p.Authors.ToLower().Contains(lowerKeyword) ||
                        p.Abstract.ToLower().Contains(lowerKeyword) ||
                        (p.Keywords != null && p.Keywords.ToLower().Contains(lowerKeyword)))
                    .Where(p => p.IsActive)
                    .OrderByDescending(p => p.Year)
                    .ToList();

                // 計算每篇文獻的讚數
                var paperIds = results.Select(p => p.PaperId).ToList();
                var likeCounts = db.Likes
                    .Where(l => paperIds.Contains(l.PaperId))
                    .GroupBy(l => l.PaperId)
                    .ToDictionary(g => g.Key, g => g.Count());

                ViewBag.LikeCounts = likeCounts;

                // 使用 ViewBag 傳遞搜尋結果
                ViewBag.SearchResults = results;
                ViewBag.ResultCount = results.Count;
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "搜尋時發生錯誤: " + ex.Message;
                ViewBag.SearchResults = new List<object>();
                ViewBag.ResultCount = 0;
            }

            return View();
        }

        // 處理舊路徑的重定向方法 - 如果不需要，可以移除
        public ActionResult List(string keyword, string sort, int page = 1)
        {
            return RedirectToAction("Papers", new { keyword, sort, page });
        }

        public ActionResult Detail(int? id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "文獻ID不能為空";
                return RedirectToAction("Papers");
            }
            return RedirectToAction("PaperDetail", new { id });
        }
    }
}