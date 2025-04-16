using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class HomeController : Controller
    {
        private readonly LiteratureEntities2 db = new LiteratureEntities2();

        public ActionResult Index(string keyword)
        {
            // 如果有提供關鍵字，進行搜尋
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                ViewBag.Keyword = keyword;

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
                        .OrderByDescending(p => p.Year)
                        .ToList();

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
            }

            // 獲取精選文獻（可選，如果您想顯示真實數據）
            try
            {
                // 查詢前3筆點擊率最高的文獻
                var featuredPapers = db.Paper
                    .Where(p => p.IsActive)
                    .OrderByDescending(p => p.ClickCount)
                    .Take(3)
                    .ToList();

                ViewBag.FeaturedPapers = featuredPapers;
            }
            catch (Exception)
            {
                // 如果發生錯誤，使用空列表
                ViewBag.FeaturedPapers = new List<object>();
            }

            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";
            return View();
        }

        public ActionResult Categories()
        {
            return View();
        }

        // 在HomeController中添加此方法，用於重定向到PaperManagement控制器
        public ActionResult PaperDetail(int? id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "文獻ID不能為空";
                return RedirectToAction("Index");
            }

            return RedirectToAction("PaperDetail", "PaperManagement", new { id = id });
        }
        public ActionResult Search(string keyword)
        {
            ViewBag.Keyword = keyword;

            if (string.IsNullOrWhiteSpace(keyword))
            {
                // 使用空列表初始化 ViewBag.SearchResults
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
                    .OrderByDescending(p => p.Year)
                    .ToList();

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

        public ActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Register(string Username, string Email, string PhoneNumber, string Password, string ConfirmPassword)
        {
            if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Email) ||
                string.IsNullOrEmpty(PhoneNumber) || string.IsNullOrEmpty(Password))
            {
                ViewBag.ErrorMessage = "所有欄位都需要填寫";
                return View();
            }

            if (Password != ConfirmPassword)
            {
                ViewBag.ErrorMessage = "密碼和確認密碼不符";
                return View();
            }

            try
            {
                var existingUser = db.Users.FirstOrDefault(u => u.Email == Email);
                if (existingUser != null)
                {
                    ViewBag.ErrorMessage = "此電子郵件已被註冊";
                    return View();
                }

                var newUser = new Users
                {
                    Username = Username,
                    Email = Email,
                    PhoneNumber = PhoneNumber,
                    Password = Password,
                    RegisterDate = DateTime.Now
                };

                db.Users.Add(newUser);
                int result = db.SaveChanges();

                if (result > 0)
                {
                    TempData["SuccessMessage"] = "註冊成功，請登入";
                    return RedirectToAction("Login");
                }
                else
                {
                    ViewBag.ErrorMessage = "註冊失敗，請稍後再試。";
                    return View();
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "註冊過程中發生錯誤: " + ex.Message;
                return View();
            }
        }

        public ActionResult Login()
        {
            if (TempData["SuccessMessage"] != null)
            {
                ViewBag.SuccessMessage = TempData["SuccessMessage"].ToString();
            }
            return View();
        }

        [HttpPost]
        public ActionResult Login(string Email, string Password, bool RememberMe = false)
        {
            if (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Password))
            {
                ViewBag.ErrorMessage = "電子郵件和密碼都需要填寫";
                return View();
            }

            try
            {
                var user = db.Users.FirstOrDefault(u => u.Email == Email && u.Password == Password);

                if (user != null)
                {
                    Session["UserId"] = user.UserId;
                    Session["Username"] = user.Username;
                    user.LastLoginDate = DateTime.Now;
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                else
                {
                    ViewBag.ErrorMessage = "電子郵件或密碼不正確";
                    return View();
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "登入過程中發生錯誤: " + ex.Message;
                return View();
            }
        }

        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Index");
        }
    }
}
