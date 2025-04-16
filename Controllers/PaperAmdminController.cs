using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class PaperAdminController : Controller
    {
        private readonly LiteratureEntities2 db = new LiteratureEntities2();

        // 檢查是否為管理員
        private ActionResult CheckAdminAccess()
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Login", "Home");
            }

            int userId = Convert.ToInt32(Session["UserId"]);
            // 假設UserId=1的用戶是管理員
            if (userId != 1)
            {
                TempData["ErrorMessage"] = "您沒有管理員權限";
                return RedirectToAction("Index", "Home");
            }

            return null;
        }

        // 文獻管理列表頁面
        public ActionResult AdminPaper()
        {
            var checkResult = CheckAdminAccess();
            if (checkResult != null)
                return checkResult;

            ViewBag.Papers = db.Paper.OrderByDescending(p => p.CreatedDate).ToList();

            // 取得分類資訊
            var paperCategories = db.PaperCategory.ToList();
            var categories = db.Category.ToList();

            var paperCategoryDict = new Dictionary<int, List<string>>();

            foreach (var paper in ViewBag.Papers)
            {
                var paperCats = paperCategories
                    .Where(pc => pc.PaperId == paper.PaperId)
                    .Select(pc => categories.FirstOrDefault(c => c.CategoryId == pc.CategoryId)?.Name)
                    .Where(name => name != null)
                    .ToList();

                paperCategoryDict[paper.PaperId] = paperCats;
            }

            ViewBag.PaperCategories = paperCategoryDict;
            ViewBag.Categories = categories;

            return View();
        }

        // 新增文獻
        [HttpPost]
        public ActionResult Paper_Add(Paper model, HttpPostedFileBase pdfFile, HttpPostedFileBase previewImage, int[] selectedCategories, string ExternalUrl = null)
        {
            var checkResult = CheckAdminAccess();
            if (checkResult != null)
                return checkResult;

            try
            {
                // 處理外部連結URL
                model.ExternalUrl = ExternalUrl;

                // 處理PDF文件上傳
                if (pdfFile != null && pdfFile.ContentLength > 0)
                {
                    string fileExtension = Path.GetExtension(pdfFile.FileName).ToLower();

                    // 檢查文件類型
                    if (fileExtension != ".pdf")
                    {
                        TempData["Message"] = "文件必須是PDF格式";
                        return RedirectToAction("AdminPaper");
                    }

                    // 生成唯一文件名
                    string uniqueFileName = Guid.NewGuid().ToString() + fileExtension;
                    string uploadPath = Path.Combine(Server.MapPath("~/Uploads/Papers"), uniqueFileName);

                    // 確保目錄存在
                    Directory.CreateDirectory(Path.GetDirectoryName(uploadPath));

                    // 保存文件
                    pdfFile.SaveAs(uploadPath);

                    // 設置文件路徑
                    model.FilePath = "/Uploads/Papers/" + uniqueFileName;
                }

                // 處理預覽圖片上傳
                if (previewImage != null && previewImage.ContentLength > 0)
                {
                    string fileExtension = Path.GetExtension(previewImage.FileName).ToLower();

                    // 檢查文件類型
                    if (fileExtension != ".jpg" && fileExtension != ".jpeg" && fileExtension != ".png")
                    {
                        TempData["Message"] = "預覽圖片必須是JPG或PNG格式";
                        return RedirectToAction("AdminPaper");
                    }

                    // 生成唯一文件名
                    string uniqueFileName = Guid.NewGuid().ToString() + fileExtension;
                    string uploadPath = Path.Combine(Server.MapPath("~/Uploads/Previews"), uniqueFileName);

                    // 確保目錄存在
                    Directory.CreateDirectory(Path.GetDirectoryName(uploadPath));

                    // 保存文件
                    previewImage.SaveAs(uploadPath);

                    // 設置圖片路徑
                    model.PreviewImagePath = "/Uploads/Previews/" + uniqueFileName;
                }

                // 設置創建日期和點擊數
                model.CreatedDate = DateTime.Now;
                model.ClickCount = 0;

                // 保存文獻
                db.Paper.Add(model);
                db.SaveChanges();

                // 處理分類關聯
                if (selectedCategories != null && selectedCategories.Length > 0)
                {
                    foreach (var categoryId in selectedCategories)
                    {
                        var paperCategory = new PaperCategory
                        {
                            PaperId = model.PaperId,
                            CategoryId = categoryId
                        };

                        db.PaperCategory.Add(paperCategory);
                    }

                    db.SaveChanges();
                }

                TempData["Message"] = "文獻已成功新增";
            }
            catch (Exception ex)
            {
                TempData["Message"] = "新增文獻時發生錯誤: " + ex.Message;
            }

            return RedirectToAction("AdminPaper");
        }

        // 編輯文獻 - 修改參數為可空類型或提供默認值
        [HttpPost]
        public ActionResult Paper_Edit(int PaperId, string Title, string Authors, string Abstract,
                                      string Journal, int Year, string Keywords, bool IsActive = false,
                                      HttpPostedFileBase pdfFile = null, HttpPostedFileBase previewImage = null,
                                      int[] selectedCategories = null, string ExternalUrl = null)
        {
            var checkResult = CheckAdminAccess();
            if (checkResult != null)
                return checkResult;

            try
            {
                // 獲取原有文獻
                var paper = db.Paper.Find(PaperId);
                if (paper == null)
                {
                    TempData["Message"] = "找不到指定的文獻";
                    return RedirectToAction("AdminPaper");
                }

                // 更新外部連結URL
                paper.ExternalUrl = ExternalUrl;

                // 處理PDF文件上傳
                if (pdfFile != null && pdfFile.ContentLength > 0)
                {
                    string fileExtension = Path.GetExtension(pdfFile.FileName).ToLower();

                    // 檢查文件類型
                    if (fileExtension != ".pdf")
                    {
                        TempData["Message"] = "文件必須是PDF格式";
                        return RedirectToAction("AdminPaper");
                    }

                    // 生成唯一文件名
                    string uniqueFileName = Guid.NewGuid().ToString() + fileExtension;
                    string uploadPath = Path.Combine(Server.MapPath("~/Uploads/Papers"), uniqueFileName);

                    // 確保目錄存在
                    Directory.CreateDirectory(Path.GetDirectoryName(uploadPath));

                    // 保存文件
                    pdfFile.SaveAs(uploadPath);

                    // 刪除舊文件（如果存在）
                    if (!string.IsNullOrEmpty(paper.FilePath))
                    {
                        string oldFilePath = Server.MapPath("~" + paper.FilePath);
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }

                    // 設置文件路徑
                    paper.FilePath = "/Uploads/Papers/" + uniqueFileName;
                }

                // 處理預覽圖片上傳
                if (previewImage != null && previewImage.ContentLength > 0)
                {
                    string fileExtension = Path.GetExtension(previewImage.FileName).ToLower();

                    // 檢查文件類型
                    if (fileExtension != ".jpg" && fileExtension != ".jpeg" && fileExtension != ".png")
                    {
                        TempData["Message"] = "預覽圖片必須是JPG或PNG格式";
                        return RedirectToAction("AdminPaper");
                    }

                    // 生成唯一文件名
                    string uniqueFileName = Guid.NewGuid().ToString() + fileExtension;
                    string uploadPath = Path.Combine(Server.MapPath("~/Uploads/Previews"), uniqueFileName);

                    // 確保目錄存在
                    Directory.CreateDirectory(Path.GetDirectoryName(uploadPath));

                    // 保存文件
                    previewImage.SaveAs(uploadPath);

                    // 刪除舊圖片（如果存在）
                    if (!string.IsNullOrEmpty(paper.PreviewImagePath))
                    {
                        string oldFilePath = Server.MapPath("~" + paper.PreviewImagePath);
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }

                    // 設置圖片路徑
                    paper.PreviewImagePath = "/Uploads/Previews/" + uniqueFileName;
                }

                // 更新文獻屬性
                paper.Title = Title;
                paper.Authors = Authors;
                paper.Abstract = Abstract;
                paper.Journal = Journal;
                paper.Year = Year;
                paper.Keywords = Keywords;
                paper.IsActive = IsActive;

                // 保存變更
                db.SaveChanges();

                // 更新分類關聯
                // 先刪除現有關聯
                var existingPaperCategories = db.PaperCategory.Where(pc => pc.PaperId == PaperId).ToList();
                foreach (var pc in existingPaperCategories)
                {
                    db.PaperCategory.Remove(pc);
                }
                db.SaveChanges();

                // 添加新關聯
                if (selectedCategories != null && selectedCategories.Length > 0)
                {
                    foreach (var categoryId in selectedCategories)
                    {
                        var paperCategory = new PaperCategory
                        {
                            PaperId = PaperId,
                            CategoryId = categoryId
                        };

                        db.PaperCategory.Add(paperCategory);
                    }

                    db.SaveChanges();
                }

                TempData["Message"] = "文獻已成功更新";
            }
            catch (Exception ex)
            {
                TempData["Message"] = "更新文獻時發生錯誤: " + ex.Message;
            }

            return RedirectToAction("AdminPaper");
        }

        // 修改刪除文獻功能，改為上架/下架切換
        [HttpPost]
        public ActionResult Paper_Delete(int PaperId)
        {
            var checkResult = CheckAdminAccess();
            if (checkResult != null)
                return checkResult;

            try
            {
                // 獲取原有文獻
                var paper = db.Paper.Find(PaperId);
                if (paper == null)
                {
                    TempData["Message"] = "找不到指定的文獻";
                    return RedirectToAction("AdminPaper");
                }

                // 將文獻標記為下架（而不是真正刪除）
                paper.IsActive = false;
                db.SaveChanges();

                TempData["Message"] = "文獻已成功下架";
            }
            catch (Exception ex)
            {
                TempData["Message"] = "下架文獻時發生錯誤: " + ex.Message;
            }

            return RedirectToAction("AdminPaper");
        }

        // 新增上架文獻功能
        [HttpPost]
        public ActionResult Paper_Publish(int PaperId)
        {
            var checkResult = CheckAdminAccess();
            if (checkResult != null)
                return checkResult;

            try
            {
                // 獲取原有文獻
                var paper = db.Paper.Find(PaperId);
                if (paper == null)
                {
                    TempData["Message"] = "找不到指定的文獻";
                    return RedirectToAction("AdminPaper");
                }

                // 將文獻標記為上架
                paper.IsActive = true;
                db.SaveChanges();

                TempData["Message"] = "文獻已成功上架";
            }
            catch (Exception ex)
            {
                TempData["Message"] = "上架文獻時發生錯誤: " + ex.Message;
            }

            return RedirectToAction("AdminPaper");
        }

        // 切換文獻上架/下架狀態
        [HttpPost]
        public ActionResult Paper_ToggleStatus(int PaperId)
        {
            var checkResult = CheckAdminAccess();
            if (checkResult != null)
                return checkResult;

            try
            {
                // 獲取原有文獻
                var paper = db.Paper.Find(PaperId);
                if (paper == null)
                {
                    TempData["Message"] = "找不到指定的文獻";
                    return RedirectToAction("AdminPaper");
                }

                // 切換文獻狀態
                paper.IsActive = !paper.IsActive;
                db.SaveChanges();

                string status = paper.IsActive ? "上架" : "下架";
                TempData["Message"] = $"文獻已成功{status}";
            }
            catch (Exception ex)
            {
                TempData["Message"] = "切換文獻狀態時發生錯誤: " + ex.Message;
            }

            return RedirectToAction("AdminPaper");
        }


        // 獲取指定文獻的分類（用於AJAX請求）
        [HttpGet]
        public JsonResult GetPaperCategories(int paperId)
        {
            var checkResult = CheckAdminAccess();
            if (checkResult != null)
                return Json(new List<int>(), JsonRequestBehavior.AllowGet);

            try
            {
                var categoryIds = db.PaperCategory
                    .Where(pc => pc.PaperId == paperId)
                    .Select(pc => pc.CategoryId)
                    .ToList();

                return Json(categoryIds, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json(new List<int>(), JsonRequestBehavior.AllowGet);
            }
        }
    }
}