'use server';

import { CreateCategoryDto, UpdateCategoryDto } from "../../types/category";
import { CategoryService } from "../services/category/service";
import { ICategoryService } from "../services/category/interface";
import { revalidatePath } from "next/cache";
import { redirect } from "next/navigation";
import fs from 'fs';
import path from 'path';
import { v4 as uuidv4 } from 'uuid';
import { requireCatalogAdmin } from '../catalog-access';
import { removeLocalCatalogImage } from '../local-catalog-images';

const categoryService: ICategoryService = new CategoryService();

async function uploadImageFile(file: File): Promise<string> {
  const useCloudflare = process.env.USE_CLOUDFLARE === 'true';

  if (useCloudflare) {
    try {
      const cfRes = await fetch(
        `https://api.cloudflare.com/client/v4/accounts/${process.env.CF_ACCOUNT_ID}/images/v2/direct_upload`,
        {
          method: 'POST',
          headers: {
            Authorization: `Bearer ${process.env.CF_API_TOKEN}`,
          },
        }
      );

      const cfJson = await cfRes.json();
      const uploadURL = cfJson.result?.uploadURL;

      if (!uploadURL) {
        throw new Error('Failed to get upload URL from Cloudflare');
      }

      const cloudflareFormData = new FormData();
      cloudflareFormData.append('file', file);

      const uploadRes = await fetch(uploadURL, {
        method: 'POST',
        body: cloudflareFormData,
      });

      const data = await uploadRes.json();

      if (!data.result || !data.result.id) {
        throw new Error('Cloudflare upload failed');
      }

      return `https://imagedelivery.net/${process.env.NEXT_PUBLIC_CF_DELIVERY_ID}/${data.result.id}/public`;

    } catch (cloudflareError) {
      console.log('Cloudflare upload failed, falling back to local:', cloudflareError);
    }
  }

  const ALLOWED_EXTENSIONS: Record<string, string> = {
    'image/jpeg': '.jpg',
    'image/png': '.png',
    'image/gif': '.gif',
    'image/webp': '.webp',
  };

  const mimeType = file.type.toLowerCase();
  if (!ALLOWED_EXTENSIONS[mimeType]) {
    throw new Error(`File type not allowed. Accepted: JPEG, PNG, GIF, WebP.`);
  }

  const MAX_SIZE = 10 * 1024 * 1024;
  if (file.size > MAX_SIZE) {
    throw new Error('Image must be smaller than 10 MB.');
  }

  const bytes = await file.arrayBuffer();
  const buffer = Buffer.from(bytes);

  const fileExtension = ALLOWED_EXTENSIONS[mimeType];
  const fileName = `${Date.now()}_category_${uuidv4()}${fileExtension}`;
  const uploadDir = path.join(process.cwd(), 'public/uploads/category');

  if (!fs.existsSync(uploadDir)) {
    fs.mkdirSync(uploadDir, { recursive: true });
  }


  const filePath = path.join(uploadDir, fileName);
  fs.writeFileSync(filePath, buffer);

  return `/uploads/category/${fileName}`;
}

export async function createCategory(formData: FormData) {
  await requireCatalogAdmin();
  const name = formData.get('name') as string;
  const description = formData.get('description') as string;
  const imageFile = formData.get('imageFile') as File;
  
  const errors: Record<string, string> = {};
  
  if (!name?.trim()) {
    errors.name = 'Category name is required';
  } else if (name.length < 2) {
    errors.name = 'Category name must be at least 2 characters';
  }
  
  if (!description?.trim()) {
    errors.description = 'Description is required';
  } else if (description.length < 10) {
    errors.description = 'Description must be at least 10 characters';
  }
  
  if (!imageFile || imageFile.size === 0) {
    errors.imageFile = 'Category image is required';
  } else {
    if (!imageFile.type.startsWith('image/')) {
      errors.imageFile = 'Please select a valid image file';
    }

    else if (imageFile.size > 10 * 1024 * 1024) {
      errors.imageFile = 'Image size must be less than 10MB';
    }
  }
  
  if (Object.keys(errors).length > 0) {
    const errorParams = new URLSearchParams();
    Object.entries(errors).forEach(([key, value]) => {
      errorParams.set(`error_${key}`, value);
    });
    errorParams.set('name', name || '');
    errorParams.set('description', description || '');
    
    redirect(`/admin/categories/create?${errorParams.toString()}`);
  }
  
  try {
    const imageUrl = await uploadImageFile(imageFile);
    
    const createData: CreateCategoryDto = {
      name: name.trim(),
      description: description.trim(),
      imageUrl: imageUrl
    };
    
    await categoryService.create(createData);
    revalidatePath('/admin/categories');
    redirect('/admin/categories?success=Category created successfully!');
  } catch (error) {
    const errorMessage = error instanceof Error ? error.message : 'An error occurred';
    if (error instanceof Error && error.message === 'NEXT_REDIRECT') {
      throw error;
    }
    redirect(`/admin/categories/create?error=${encodeURIComponent(errorMessage)}&name=${encodeURIComponent(name || '')}&description=${encodeURIComponent(description || '')}`);
  }
}

export async function updateCategory(formData: FormData, _currentImageUrl: string) {
  await requireCatalogAdmin();
  const id = formData.get('id') as string;
  const name = formData.get('name') as string;
  const description = formData.get('description') as string;
  const imageFile = formData.get('imageFile') as File;
  const keepCurrentImage = formData.get('keepCurrentImage') as string;
  
  const errors: Record<string, string> = {};
  
  if (!id) {
    redirect('/admin/categories?error=Invalid category ID');
  }
  
  if (!name?.trim()) {
    errors.name = 'Category name is required';
  } else if (name.length < 2) {
    errors.name = 'Category name must be at least 2 characters';
  }
  
  if (!description?.trim()) {
    errors.description = 'Description is required';
  } else if (description.length < 10) {
    errors.description = 'Description must be at least 10 characters';
  }
  
  const hasNewImage = imageFile && imageFile.size > 0;
  const shouldKeepCurrent = keepCurrentImage === 'true';
  
  if (!hasNewImage && !shouldKeepCurrent) {
    errors.imageFile = 'Please upload a new image or check "Keep current image"';
  } else if (hasNewImage) {
    if (!imageFile.type.startsWith('image/')) {
      errors.imageFile = 'Please select a valid image file';
    } else if (imageFile.size > 10 * 1024 * 1024) {
      errors.imageFile = 'Image size must be less than 10MB';
    }
  }
  
  if (Object.keys(errors).length > 0) {
    const errorParams = new URLSearchParams();
    Object.entries(errors).forEach(([key, value]) => {
      errorParams.set(`error_${key}`, value);
    });
    errorParams.set('name', name || '');
    errorParams.set('description', description || '');
    
    redirect(`/admin/categories/update/${id}?${errorParams.toString()}`);
  }
  
  try {
    let imageUrl: string;
    
    if (hasNewImage) {
      imageUrl = await uploadImageFile(imageFile);
    } else {
      const existingCategory = await categoryService.getById(parseInt(id));
      if (!existingCategory) {
        redirect(`/admin/categories/update/${id}?error=Category not found`);
      }
      imageUrl = existingCategory.imageUrl;
    }
    
    const updateData: UpdateCategoryDto = {
      id: parseInt(id),
      name: name.trim(),
      description: description.trim(),
      imageUrl: imageUrl
    };
    
    await categoryService.update(parseInt(id), updateData);
    revalidatePath('/admin/categories');
    redirect('/admin/categories?success=Category updated successfully!');
  } catch (error) {
    const errorMessage = error instanceof Error ? error.message : 'An error occurred';
    if (error instanceof Error && error.message === 'NEXT_REDIRECT') {
      throw error;
    }
    redirect(`/admin/categories/update/${id}?error=${encodeURIComponent(errorMessage)}&name=${encodeURIComponent(name || '')}&description=${encodeURIComponent(description || '')}`);
  }
}

export async function validateConfirmation(formData: FormData) {
  const confirmationText = formData.get('confirmationText') as string;
  const categoryId = formData.get('categoryId') as string;
  const expectedConfirmation = 'DELETE';
  
  console.log('Validation attempt:', { confirmationText, categoryId, expectedConfirmation });
  
  if (!categoryId) {
    redirect('/admin/categories?error=Invalid category ID');
  }
  
  if (!confirmationText || confirmationText.trim() !== expectedConfirmation) {
    const errorParam = confirmationText 
      ? 'Please type "DELETE" exactly as shown (case sensitive)'
      : 'Please type "DELETE" to confirm';
    
    const params = new URLSearchParams({
      error: errorParam,
      attempted: 'true'
    });
    
    redirect(`/admin/categories/delete/${categoryId}?${params.toString()}`);
  }
  
  const params = new URLSearchParams({
    confirmed: 'true'
  });
  
  redirect(`/admin/categories/delete/${categoryId}?${params.toString()}`);
}

export async function deleteCategory(formData: FormData) {
  await requireCatalogAdmin();
  const categoryId = parseInt(formData.get('id') as string);
  
  console.log('Delete attempt for category ID:', categoryId);
  
  if (isNaN(categoryId)) {
    redirect('/admin/categories?error=Invalid category ID');
  }
  
  try {
    const categoryExists = await categoryService.existsById(categoryId);
    console.log('Category exists:', categoryExists);
    
    if (!categoryExists) {
      redirect(`/admin/categories/delete/${categoryId}?error=Category not found&confirmed=true`);
    }
    
    const category = await categoryService.getById(categoryId);

    const success = await categoryService.delete(categoryId);
    console.log('Delete result:', success);
    
    if (!success) {
      redirect(`/admin/categories/delete/${categoryId}?error=Failed to delete category - operation returned false&confirmed=true`);
    }
    
    if (category && category.imageUrl.startsWith('/uploads/')) {
      try {
        removeLocalCatalogImage(category.imageUrl, 'category');
      } catch (cleanupError) {
        console.error('Failed to cleanup image file:', cleanupError);
      }
    }
    
    revalidatePath('/admin/categories');
    revalidatePath(`/admin/categories/delete/${categoryId}`);
    
    redirect('/admin/categories?success=Category deleted successfully&deleted=true');
    
  } catch (error) {
    console.error('Error deleting category:', error);
    
    if (error instanceof Error && error.message === 'NEXT_REDIRECT') {
      throw error;
    }
    
    const errorMessage = error instanceof Error ? error.message : 'Failed to delete category';
    console.error('Redirecting with error:', errorMessage);
    
    redirect(`/admin/categories/delete/${categoryId}?error=${encodeURIComponent(errorMessage)}&confirmed=true`);
  }
}