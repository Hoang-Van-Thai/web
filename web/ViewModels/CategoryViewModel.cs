using System.ComponentModel.DataAnnotations;

namespace web.ViewModels
{
    public class CategoryViewModel
    {
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mã danh mục")]
        [Display(Name = "Mã danh mục")]
        public string CategoryCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập tên danh mục")]
        [Display(Name = "Tên danh mục")]
        public string CategoryName { get; set; } = string.Empty;
    }
}