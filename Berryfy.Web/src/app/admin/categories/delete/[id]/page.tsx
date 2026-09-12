import { notFound } from 'next/navigation';
import DeleteCategoryForm from '../../../../../components/category/DeleteCategoryForm';
import { CategoryService } from '../../../../../lib/services/category/service';
import { ICategoryService } from "../../../../../lib/services/category/interface";

const categoryService: ICategoryService = new CategoryService();

interface Props {
  params: Promise<{
    id: string;
  }>;
  searchParams?: Promise<{ [key: string]: string | string[] | undefined }>;
}

export default async function DeleteCategoryPage({ params, searchParams }: Props) {
  const resolvedSearchParams = await searchParams;
  const { id } = await params;
  const categoryId = parseInt(id);

  if (isNaN(categoryId)) {
    notFound();
  }

  let category;
  try {
    category = await categoryService.getById(categoryId);
  } catch (error) {
    console.error("Failed to load category:", error);
    notFound();
  }

  return (
    <div className="container-fluid">
      <div className="row mb-4">
        <div className="col-12">
          <div className="d-flex align-items-center">
            <a
              href="/admin/categories"
              className="btn btn-outline-secondary me-3"
              title="Back to Categories"
            >
              <i className="bi bi-arrow-left"></i>
            </a>
            <div>
              <h1 className="h2 mb-1">
                <i className="bi bi-trash me-2 text-danger"></i>
                Delete Category
              </h1>
              <p className="text-muted mb-0">
                Permanently remove &quot;{category.name}&quot; from the system
              </p>
            </div>
          </div>
        </div>
      </div>

      <div className="row justify-content-center mb-4">
        <div className="col-lg-8 col-xl-6">
          <div className="alert alert-warning d-flex align-items-center" role="alert">
            <i className="bi bi-exclamation-triangle-fill me-2"></i>
            <div>
              <strong>Warning!</strong> This action cannot be undone. The category and all associated data will be permanently deleted.
            </div>
          </div>
        </div>
      </div>

      <div className="row justify-content-center">
        <div className="col-lg-8 col-xl-6">
          <div className="card shadow-sm border-0">
            <div className="card-header bg-white py-3">
              <h5 className="card-title mb-0">
                <i className="bi bi-info-circle me-2"></i>
                Category Details
              </h5>
            </div>
            <div className="card-body p-4">
              <div className="row mb-4">
                <div className="col-md-4">
                  <img
                    src={category.imageUrl}
                    alt={category.name}
                    className="img-fluid rounded"
                    style={{ width: '100%', height: '150px', objectFit: 'cover' }}
                  />
                </div>
                <div className="col-md-8">
                  <h5 className="mb-2">{category.name}</h5>
                  <p className="text-muted mb-2">
                    <strong>ID:</strong> {category.id}
                  </p>
                  <p className="text-muted">
                    {category.description}
                  </p>
                </div>
              </div>

              <hr className="my-4" />

              <DeleteCategoryForm category={category} searchParams={resolvedSearchParams} />
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}