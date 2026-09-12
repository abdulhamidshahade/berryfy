import { CategoryService } from '../../lib/services/category/service';
import { ICategoryService } from '../../lib/services/category/interface';
import { CategoryDto } from '../../types/category';
import { deleteCategory, validateConfirmation } from '../../lib/actions/category-actions';

interface Props {
  category: CategoryDto;
  searchParams?: { [key: string]: string | string[] | undefined };
}

const categoryService: ICategoryService = new CategoryService();

export default function DeleteCategoryForm({ category, searchParams }: Props) {
  const expectedConfirmation = 'DELETE';
  
  const error = searchParams?.error as string;
  const attempted = searchParams?.attempted === 'true';
  const confirmed = searchParams?.confirmed === 'true';

  return (
    <div>
      {error && (
        <div className="alert alert-danger d-flex align-items-center mb-4" role="alert">
          <i className="bi bi-exclamation-triangle-fill me-2"></i>
          <div>{error}</div>
          <a 
            href={`/admin/categories/delete/${category.id}`} 
            className="btn btn-sm btn-outline-secondary ms-auto"
          >
            <i className="bi bi-x me-1"></i>
            Dismiss
          </a>
        </div>
      )}

      {!confirmed ? (
        <form action={validateConfirmation}>
          <input type="hidden" name="categoryId" value={category.id} />
          <div className="mb-4">
            <h6 className="fw-semibold mb-3">
              <i className="bi bi-shield-exclamation me-2"></i>
              Confirmation Required
            </h6>
            <p className="text-muted mb-3">
              To confirm deletion, please type <strong>{expectedConfirmation}</strong> in the field below:
            </p>
            <input
              type="text"
              name="confirmationText"
              className={`form-control ${attempted && error ? 'is-invalid' : ''}`}
              placeholder={`Type "${expectedConfirmation}" to confirm`}
              required
              autoComplete="off"
            />
            {attempted && error && (
              <div className="invalid-feedback">
                Please type &quot;{expectedConfirmation}&quot; exactly as shown
              </div>
            )}
          </div>

          <div className="d-flex gap-3">
            <button
              type="submit"
              className="btn btn-outline-danger flex-fill"
            >
              <i className="bi bi-check-circle me-2"></i>
              Verify Confirmation
            </button>
            
            <a
              href="/admin/categories"
              className="btn btn-outline-secondary"
            >
              <i className="bi bi-x-circle me-2"></i>
              Cancel
            </a>
          </div>
        </form>
      ) : (
        <div>
          <div className="alert alert-success d-flex align-items-center mb-4" role="alert">
            <i className="bi bi-check-circle-fill me-2"></i>
            <div>
              <strong>Confirmation verified!</strong> You can now proceed with the deletion.
            </div>
          </div>

          <form action={deleteCategory}>
            <input type="hidden" name="id" value={category.id} />
            <div className="mb-4">
              <h6 className="fw-semibold mb-3 text-success">
                <i className="bi bi-shield-check me-2"></i>
                Ready to Delete
              </h6>
              <p className="text-muted mb-3">
                Confirmation received. Click the button below to permanently delete the category.
              </p>
            </div>

            <div className="d-flex gap-3">
              <button
                type="submit"
                className="btn btn-danger flex-fill"
              >
                <i className="bi bi-trash me-2"></i>
                Delete Category Now
              </button>
              
              <a
                href="/admin/categories"
                className="btn btn-outline-secondary"
              >
                <i className="bi bi-x-circle me-2"></i>
                Cancel
              </a>
            </div>
          </form>
        </div>
      )}

      <div className="mt-4 p-3 bg-light rounded">
        <h6 className="text-danger mb-2">
          <i className="bi bi-exclamation-triangle me-2"></i>
          This action will:
        </h6>
        <ul className="text-muted mb-0 small">
          <li>Permanently delete the category &quot;{category.name}&quot;</li>
          <li>Remove all references to this category</li>
          <li>This action cannot be undone</li>
        </ul>
      </div>
    </div>
  );
}