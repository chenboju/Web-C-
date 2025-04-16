using System;
using System.Linq;
using System.Web.Mvc;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class AccountController : Controller
    {
        private readonly LiteratureEntities2 db = new LiteratureEntities2();

        // 檢查用戶是否已登入
        private ActionResult CheckLogin()
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Login", "Home");
            }
            return null;
        }

        // 個人資料頁面
        public ActionResult Profile()
        {
            var checkResult = CheckLogin();
            if (checkResult != null)
                return checkResult;

            try
            {
                int userId = Convert.ToInt32(Session["UserId"]);
                var userProfile = db.Users.FirstOrDefault(u => u.UserId == userId);

                if (userProfile == null)
                {
                    return RedirectToAction("Logout", "Home");
                }

                ViewBag.UserProfile = userProfile;
                return View();
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "取得個人資料時發生錯誤: " + ex.Message;
                return View();
            }
        }

        // 更新個人資料
        [HttpPost]
        public ActionResult UpdateProfile(string Username, string Email, string PhoneNumber)
        {
            var checkResult = CheckLogin();
            if (checkResult != null)
                return checkResult;

            if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(PhoneNumber))
            {
                TempData["ErrorMessage"] = "所有欄位都需要填寫";
                return RedirectToAction("Profile");
            }

            try
            {
                int userId = Convert.ToInt32(Session["UserId"]);
                var user = db.Users.FirstOrDefault(u => u.UserId == userId);

                if (user == null)
                {
                    return RedirectToAction("Logout", "Home");
                }

                // 檢查電子郵件是否已被其他用戶使用
                var existingUser = db.Users.FirstOrDefault(u => u.Email == Email && u.UserId != userId);
                if (existingUser != null)
                {
                    TempData["ErrorMessage"] = "此電子郵件已被其他用戶使用";
                    return RedirectToAction("Profile");
                }

                // 更新用戶資料
                user.Username = Username;
                user.Email = Email;
                user.PhoneNumber = PhoneNumber;

                db.SaveChanges();

                // 更新Session中的用戶名
                Session["Username"] = Username;

                TempData["SuccessMessage"] = "個人資料已成功更新";
                return RedirectToAction("Profile");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "更新個人資料時發生錯誤: " + ex.Message;
                return RedirectToAction("Profile");
            }
        }

        // 修改密碼頁面
        public ActionResult ChangePassword()
        {
            var checkResult = CheckLogin();
            if (checkResult != null)
                return checkResult;

            return View();
        }

        // 更新密碼
        [HttpPost]
        public ActionResult UpdatePassword(string CurrentPassword, string NewPassword, string ConfirmPassword)
        {
            var checkResult = CheckLogin();
            if (checkResult != null)
                return checkResult;

            if (string.IsNullOrEmpty(CurrentPassword) || string.IsNullOrEmpty(NewPassword) || string.IsNullOrEmpty(ConfirmPassword))
            {
                TempData["ErrorMessage"] = "所有密碼欄位都需要填寫";
                return RedirectToAction("ChangePassword");
            }

            if (NewPassword != ConfirmPassword)
            {
                TempData["ErrorMessage"] = "新密碼和確認密碼不符";
                return RedirectToAction("ChangePassword");
            }

            if (NewPassword.Length < 6)
            {
                TempData["ErrorMessage"] = "新密碼長度不得少於6個字符";
                return RedirectToAction("ChangePassword");
            }

            try
            {
                int userId = Convert.ToInt32(Session["UserId"]);
                var user = db.Users.FirstOrDefault(u => u.UserId == userId);

                if (user == null)
                {
                    return RedirectToAction("Logout", "Home");
                }

                // 驗證當前密碼
                if (user.Password != CurrentPassword)
                {
                    TempData["ErrorMessage"] = "當前密碼不正確";
                    return RedirectToAction("ChangePassword");
                }

                // 更新密碼
                user.Password = NewPassword;
                db.SaveChanges();

                TempData["SuccessMessage"] = "密碼已成功更新";
                return RedirectToAction("ChangePassword");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "更新密碼時發生錯誤: " + ex.Message;
                return RedirectToAction("ChangePassword");
            }
        }
    }
}