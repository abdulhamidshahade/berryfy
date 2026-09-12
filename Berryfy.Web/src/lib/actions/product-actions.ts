"use server";

import { revalidatePath } from "next/cache";
import { redirect } from "next/navigation";
import { ProductService } from "../services/product/service";
import { CreateProductDto, UpdateProductDto } from "../../types/product";
import { IProductService } from "../services/product/interface";
import fs from "fs";
import path from "path";
import { v4 as uuidv4 } from "uuid";
import { ProductFilterDto, PaginationDto } from "../../types/pagination";
import { requireCatalogAdmin } from '../catalog-access';
import { removeLocalCatalogImage } from '../local-catalog-images';

const productService: IProductService = new ProductService();

async function uploadImageFile(
  file: File
): Promise<string> {
  const useCloudflare = process.env.USE_CLOUDFLARE === "true";

  if (useCloudflare) {
    try {
      const cfRes = await fetch(
        `https://api.cloudflare.com/client/v4/accounts/${process.env.CF_ACCOUNT_ID}/images/v2/direct_upload`,
        {
          method: "POST",
          headers: {
            Authorization: `Bearer ${process.env.CF_API_TOKEN}`,
          },
        }
      );

      const cfJson = await cfRes.json();
      const uploadURL = cfJson.result?.uploadURL;

      if (!uploadURL) {
        throw new Error("Failed to get upload URL from Cloudflare");
      }

      const cloudflareFormData = new FormData();
      cloudflareFormData.append("file", file);

      const uploadRes = await fetch(uploadURL, {
        method: "POST",
        body: cloudflareFormData,
      });

      const data = await uploadRes.json();

      if (!data.result || !data.result.id) {
        throw new Error("Cloudflare upload failed");
      }

      return `https://imagedelivery.net/${process.env.NEXT_PUBLIC_CF_DELIVERY_ID}/${data.result.id}/public`;
    } catch (cloudflareError) {
      console.log(
        "Cloudflare upload failed, falling back to local:",
        cloudflareError
      );
    }
  }

  const ALLOWED_EXTENSIONS: Record<string, string> = {
    "image/jpeg": ".jpg",
    "image/png": ".png",
    "image/gif": ".gif",
    "image/webp": ".webp",
  };

  const mimeType = file.type.toLowerCase();
  if (!ALLOWED_EXTENSIONS[mimeType]) {
    throw new Error(`File type not allowed. Accepted: JPEG, PNG, GIF, WebP.`);
  }

  const MAX_SIZE = 10 * 1024 * 1024;
  if (file.size > MAX_SIZE) {
    throw new Error("Image must be smaller than 10 MB.");
  }

  const bytes = await file.arrayBuffer();
  const buffer = Buffer.from(bytes);

  const fileExtension = ALLOWED_EXTENSIONS[mimeType];
  const fileName = `${Date.now()}_product_${uuidv4()}${fileExtension}`;
  const uploadDir = path.join(process.cwd(), "public/uploads/product");

  if (!fs.existsSync(uploadDir)) {
    fs.mkdirSync(uploadDir, { recursive: true });
  }

  const filePath = path.join(uploadDir, fileName);
  fs.writeFileSync(filePath, buffer);

  return `/uploads/product/${fileName}`;
}

export async function createProduct(formData: FormData): Promise<void> {
  await requireCatalogAdmin();
  try {
    //get -> for one element/ getAll -> for multiple elements
    const categories = formData.getAll("categories");
    const categoryIds = categories
      ? categories
        .map((id) => parseInt(id as string))
        .filter((id) => !isNaN(id))
      : [];
    const imageFile = formData.get("imageFile") as File;

    const errors: Record<string, string> = {};

    if (!imageFile || imageFile.size === 0) {
      errors.imageFile = "Product image is required";
    } else {
      if (!imageFile.type.startsWith("image/")) {
        errors.imageFile = "Please select a valid image file";
      } else if (imageFile.size > 10 * 1024 * 1024) {
        errors.imageFile = "Image size must be less than 10MB";
      }
    }

    if (Object.keys(errors).length > 0) {
      throw new Error(Object.values(errors)[0]);
    }

    const imageUrl = await uploadImageFile(imageFile);

    const productData: CreateProductDto = {
      name: formData.get("name") as string,
      description: formData.get("description") as string,
      price: parseFloat(formData.get("price") as string),
      stockQuantity: parseInt(formData.get("stockQuantity") as string),
      imageUrl: imageUrl,
      reservedStock: parseInt(formData.get("reservedStock") as string) || 0,
      lowStockThreshold:
        parseInt(formData.get("lowStockThreshold") as string) || 10,
      isActive: formData.get("isActive") === "on",
      sku: formData.get("sku") as string,
    };

    await productService.create(productData, categoryIds);

    revalidatePath("/admin/products");
    revalidatePath("/products");
    redirect("/admin/products?success=created");
  } catch (error) {
    if (error instanceof Error && error.message === "NEXT_REDIRECT") {
      throw error;
    }
    console.error("Error creating product:", error);
    throw new Error("Failed to create product");
  }
}

export async function updateProduct(
  formData: FormData,
  _currentImageUrl: string
) {
  await requireCatalogAdmin();
  try {
    const id = parseInt(formData.get("id") as string);
    const categories = formData.getAll("categories");
    const categoryIds = categories
      ? categories
        .map((id) => parseInt(id as string))
        .filter((id) => !isNaN(id))
      : [];

    const imageFile = formData.get("imageFile") as File;

    if (imageFile && imageFile.size > 0) {
      if (!imageFile.type.startsWith("image/")) {
        throw new Error("Please select a valid image file");
      }
      if (imageFile.size > 10 * 1024 * 1024) {
        throw new Error("Image size must be less than 10MB");
      }
    }

    const imageUrl = imageFile && imageFile.size > 0
      ? await uploadImageFile(imageFile)
      : (await productService.getById(id)).imageUrl;

    const productData: UpdateProductDto = {
      id,
      name: formData.get("name") as string,
      description: formData.get("description") as string,
      price: parseFloat(formData.get("price") as string),
      stockQuantity: parseInt(formData.get("stockQuantity") as string),
      imageUrl: imageUrl,
      reservedStock: parseInt(formData.get("reservedStock") as string) || 0,
      lowStockThreshold:
        parseInt(formData.get("lowStockThreshold") as string) || 10,
      isActive: formData.get("isActive") === "on",
      sku: formData.get("sku") as string,
    };

    await productService.update(id, productData, categoryIds);

    revalidatePath("/admin/products");
    revalidatePath("/products");
    redirect("/admin/products?success=updated");
  } catch (error) {
    if (error instanceof Error && error.message === "NEXT_REDIRECT") {
      throw error;
    }
    console.error("Error updating product:", error);
    throw new Error("Failed to update product");
  }
}

export async function deleteProduct(formData: FormData) {
  await requireCatalogAdmin();
  try {
    const id = parseInt(formData.get("id") as string);

    const product = await productService.getById(id);

    if (!await productService.delete(id)) throw new Error('Product could not be deleted');



    if (product && product.imageUrl.startsWith('/uploads/')) {
      try {
        removeLocalCatalogImage(product.imageUrl, 'product');
      } catch (cleanupError) {
        console.error('Failed to cleanup image file:', cleanupError);
      }
    }

    revalidatePath("/admin/products");
    revalidatePath("/products");
    redirect("/admin/products?success=deleted");
  } catch (error) {
    if (error instanceof Error && error.message === "NEXT_REDIRECT") {
      throw error;
    }
    console.error("Error deleting product:", error);
    throw new Error("Failed to delete product");
  }
}

export async function getProducts() {
  try {
    return await productService.getAll();
  } catch (error) {
    console.error("Error fetching products:", error);
    return [];
  }
}

export async function getPaginatedProducts(filter: ProductFilterDto): Promise<PaginationDto<any>> {
  try {
    return await productService.getPaginated(filter);
  } catch (error) {
    console.error("Error fetching paginated products:", error);
    return {
      data: [],
      pageNumber: 1,
      pageSize: filter.pageSize || 12,
      totalCount: 0,
      totalPages: 0,
      hasPreviousPage: false,
      hasNextPage: false,
      firstItemOnPage: 0,
      lastItemOnPage: 0
    };
  }
}

export async function getProduct(id: number) {
  try {
    return await productService.getById(id);
  } catch (error) {
    console.error("Error fetching product:", error);
    return null;
  }
}