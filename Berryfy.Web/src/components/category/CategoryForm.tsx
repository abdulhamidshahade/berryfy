import { CategoryDto } from "../../types/category";

interface Props {
  category?: CategoryDto;
  action: (formData: FormData) => Promise<void>;
  isEdit?: boolean;
  submitText: string;
  searchParams?: { [key: string]: string | string[] | undefined };
}

export default function CategoryForm({
  category,
  action,
  isEdit = false,
  submitText,
  searchParams,
}: Props) {
  const errorName = searchParams?.error_name as string;
  const errorDescription = searchParams?.error_description as string;
  const errorImageFile = searchParams?.error_imageFile as string;
  const generalError = searchParams?.error as string;

  const preservedName = searchParams?.name as string;
  const preservedDescription = searchParams?.description as string;

  return (
    <div className="container-fluid">
      <div className="row justify-content-center">
        <div className="col-lg-8">
          <div className="card shadow">
            <div className="card-header bg-primary text-white">
              <h4 className="mb-0">
                <i
                  className={`bi ${
                    isEdit ? "bi-pencil-square" : "bi-plus-circle"
                  } me-2`}
                ></i>
                {isEdit ? "Edit Category" : "Create New Category"}
              </h4>
            </div>
            <div className="card-body">
              {generalError && (
                <div className="alert alert-danger mb-3" role="alert">
                  <i className="bi bi-exclamation-triangle-fill me-2"></i>
                  {generalError}
                </div>
              )}

              <form action={action}>
                {isEdit && category && (
                  <input type="hidden" name="id" value={category.id} />
                )}

                <div className="mb-3">
                  <label htmlFor="name" className="form-label">
                    <i className="bi bi-tag me-1"></i>Category Name *
                  </label>
                  <input
                    type="text"
                    className={`form-control ${errorName ? "is-invalid" : ""}`}
                    id="name"
                    name="name"
                    defaultValue={preservedName || category?.name || ""}
                    required
                    placeholder="Enter category name"
                  />
                  {errorName && (
                    <div className="invalid-feedback">{errorName}</div>
                  )}
                </div>

                <div className="mb-3">
                  <label htmlFor="description" className="form-label">
                    <i className="bi bi-card-text me-1"></i>Description *
                  </label>
                  <textarea
                    className={`form-control ${
                      errorDescription ? "is-invalid" : ""
                    }`}
                    id="description"
                    name="description"
                    rows={4}
                    defaultValue={
                      preservedDescription || category?.description || ""
                    }
                    required
                    placeholder="Enter category description"
                  ></textarea>
                  {errorDescription && (
                    <div className="invalid-feedback">{errorDescription}</div>
                  )}
                </div>


                {isEdit && category?.imageUrl && (
                  <div className="mb-3">
                    <label className="form-label">
                      <i className="bi bi-image me-1"></i>Current Image
                    </label>
                    <div className="border rounded p-3 bg-light">
                      <img
                        src={category.imageUrl}
                        alt="Current category image"
                        className="img-thumbnail mb-2"
                        style={{
                          maxWidth: "200px",
                          maxHeight: "150px",
                          objectFit: "cover",
                        }}
                      />
                      <div className="form-check">
                        <input
                          className="form-check-input"
                          type="checkbox"
                          name="keepCurrentImage"
                          value="true"
                          id="keepCurrentImage"
                        />
                        <label
                          className="form-check-label"
                          htmlFor="keepCurrentImage"
                        >
                          Keep current image (don&apos;t upload a new one)
                        </label>
                      </div>
                    </div>
                  </div>
                )}

                <div className="mb-3">
                  <label htmlFor="imageFile" className="form-label">
                    <i className="bi bi-image me-1"></i>
                    {isEdit ? "New Category Image" : "Category Image"} *
                  </label>
                  <input
                    type="file"
                    className={`form-control ${
                      errorImageFile ? "is-invalid" : ""
                    }`}
                    id="imageFile"
                    name="imageFile"
                    accept="image/*"
                    required={!isEdit}
                  />
                  <div className="form-text">
                    <i className="bi bi-info-circle me-1"></i>
                    Upload JPG, PNG, GIF, or WebP (max 5MB)
                    {isEdit && " - Leave empty to keep current image"}
                  </div>
                  {errorImageFile && (
                    <div className="invalid-feedback">{errorImageFile}</div>
                  )}
                </div>

                <div className="d-flex gap-2 justify-content-end pt-3 border-top">
                  <a href="/admin/categories" className="btn btn-secondary">
                    <i className="bi bi-x-circle me-1"></i>Cancel
                  </a>
                  <button
                    type="submit"
                    className={`btn ${isEdit ? "btn-warning" : "btn-success"}`}
                  >
                    <i
                      className={`bi ${
                        isEdit ? "bi-pencil-square" : "bi-plus-circle"
                      } me-1`}
                    ></i>
                    {submitText}
                  </button>
                </div>
              </form>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
