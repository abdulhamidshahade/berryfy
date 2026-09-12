import CategoryList from "../../components/category/CategoryList";
import { CategoryService } from "../../lib/services/category/service";
import { ICategoryService } from "../../lib/services/category/interface";
import { CategoryDto } from "../../types/category";

const categoryService: ICategoryService = new CategoryService();

export default async function CategoriesPage() {
  let categories: CategoryDto[] = [];
  let hasError = false;

  try {
    categories = await categoryService.getAll();
  } catch (error) {
    console.error("Failed to load categories:", error);
    hasError = true;
  }

  return (
    <>
      <div className="bg-primary text-white py-5 mb-5">
        <div className="container">
          <div className="row align-items-center">
            <div className="col-lg-8">
              <h1 className="display-4 fw-bold mb-3">
                <i className="bi bi-grid-3x3-gap-fill me-3"></i>
                Categories
              </h1>
              <p className="lead mb-0">
                Discover our wide range of categories and find exactly what you&apos;re looking for.
              </p>
            </div>
            <div className="col-lg-4 text-lg-end">
              <div className="badge bg-light text-primary fs-6 px-3 py-2">
                {categories.length} Categories Available
              </div>
            </div>
          </div>
        </div>
      </div>

      <div className="container">
        {hasError && (
          <div className="row mb-4">
            <div className="col-12">
              <div className="alert alert-danger d-flex align-items-center" role="alert">
                <i className="bi bi-exclamation-triangle-fill me-2"></i>
                <div>
                  <strong>Oops!</strong> We encountered an issue loading the categories. 
                  Please try refreshing the page or contact support if the problem persists.
                </div>
              </div>
            </div>
          </div>
        )}

        <div className="row mb-4">
          <div className="col-lg-6">
            <div className="input-group">
              <span className="input-group-text">
                <i className="bi bi-search"></i>
              </span>
              <input 
                type="text" 
                className="form-control" 
                placeholder="Search categories..."
                disabled
              />
            </div>
          </div>
          <div className="col-lg-6 text-lg-end mt-3 mt-lg-0">
            <div className="btn-group" role="group">
              <button type="button" className="btn btn-outline-secondary active">
                <i className="bi bi-grid-3x3-gap"></i>
              </button>
              <button type="button" className="btn btn-outline-secondary">
                <i className="bi bi-list"></i>
              </button>
            </div>
          </div>
        </div>

        <CategoryList categories={categories} />

        {categories.length > 0 && (
          <div className="row mt-5">
            <div className="col-12 text-center">
              <button className="btn btn-outline-primary btn-lg" disabled>
                <i className="bi bi-arrow-clockwise me-2"></i>
                Load More Categories
              </button>
            </div>
          </div>
        )}
      </div>
    </>
  );
}