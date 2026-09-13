"use server";

import { revalidatePath } from "next/cache";
import { redirect, unstable_rethrow } from "next/navigation";
import { CartService } from "../services/cart/service";
import { ICartService } from "../services/cart/interface";
import { cookies } from "next/headers";
import { ProductService } from "../services/product/service";
import { AddToCartDto, CartDto } from "../../types/cart";

const cartService: ICartService = new CartService();
const productService = new ProductService();

export async function getOrCreateCart() {
  const cookieStore = cookies();

  const sessionId = (await cookieStore).get("cart_session")?.value;
  let cart = null;

  if (sessionId) {
    cart = await cartService.getBySessionId(sessionId);
  } else {
    cart = await cartService.getByUserId();
  }

  if(!cart){
    cart = await cartService.getByUserId();
  }
  return cart;
}

export async function getCart() {
  try {
    return await getOrCreateCart();
  } catch (error) {
    unstable_rethrow(error);
    console.error("Error getting cart:", error);
    return null;
  }
}

export async function addToCart(formData: FormData): Promise<CartDto> {
  try {
    const productId = parseInt(formData.get("productId") as string);
    const quantity = parseInt(formData.get("quantity") as string) || 1;

    const product = await productService.getById(productId);
    if (!product || !product.isActive) {
      throw new Error("Product not available");
    }

    const available = product.stockQuantity - product.reservedStock;
    if (available < quantity) {
      throw new Error("Insufficient stock");
    }

    const cart = await getOrCreateCart();


    const itemToAdd: AddToCartDto = {
      quantity: quantity,
      sessionId: cart.sessionId,
      productId: productId,
      userId: cart.userId,
      cartId: cart.id,
    };

    const updatedCart = await cartService.addItem(itemToAdd);

    revalidatePath("/cart");
    revalidatePath("/products");
    revalidatePath(`/products/${productId}`);
    revalidatePath("/");

    return updatedCart;
  } catch (error) {
    console.error("Error adding to cart:", error);
    throw new Error(
      error instanceof Error ? error.message : "Failed to add item to cart"
    );
  }
}

export async function updateCartItem(formData: FormData) {
  try {
    const productId = parseInt(formData.get("productId") as string);
    const quantity = parseInt(formData.get("quantity") as string);

    if (quantity < 0) {
      throw new Error("Quantity cannot be negative");
    }

    const cart = await getOrCreateCart();

    if (quantity === 0) {
      await cartService.removeItem(cart.id, productId);
    } else {
      const product = await productService.getById(productId);
      if (!product || !product.isActive) {
        throw new Error("Product not available");
      }

      const itemToUpdate = {
        productId: productId,
        quantity: quantity,
        userId: cart.userId,
        sessionId: cart.sessionId
      };
      await cartService.updateItemQuantity(cart.id, itemToUpdate);
    }

    revalidatePath("/cart");
  } catch (error) {
    console.error("Error updating cart item:", error);
    throw new Error(
      error instanceof Error ? error.message : "Failed to update cart item"
    );
  }
}

export async function removeFromCart(formData: FormData) {
  try {
    const productId = parseInt(formData.get("productId") as string);

    const cart = await getOrCreateCart();
    await cartService.removeItem(cart.id, productId);

    revalidatePath("/cart");
  } catch (error) {
    console.error("Error removing from cart:", error);
    throw new Error("Failed to remove item from cart");
  }
}

export async function clearCart() {
  try {
    const cart = await getOrCreateCart();
    await cartService.clearCart(cart.id);
    revalidatePath("/cart");
    redirect("/cart");
  } catch (error) {
    if (error instanceof Error && error.message === "NEXT_REDIRECT") {
      throw error;
    }
    console.error("Error clearing cart:", error);
    redirect("/cart?error=Failed+to+clear+cart");
  }
}

export type CouponActionState = {
  error?: string;
  success?: string;
} | null;

export async function applyCoupon(
  _prevState: CouponActionState,
  formData: FormData
): Promise<CouponActionState> {
  try {
    const couponCode = (formData.get("couponCode") as string)?.trim();

    if (!couponCode) {
      return { error: "Please enter a coupon code." };
    }

    const cart = await getOrCreateCart();

    if (!cart) {
      return { error: "Could not find your cart. Please refresh and try again." };
    }

    await cartService.applyCoupon(cart.id, { couponCode });
    revalidatePath("/cart");
    return { success: `Coupon "${couponCode}" applied successfully!` };
  } catch (error) {
    console.error("Error applying coupon:", error);
    const message = error instanceof Error ? error.message : "Failed to apply coupon";
    return { error: message };
  }
}

export async function removeCoupon(formData: FormData) {
  try {
    const couponId = parseInt(formData.get("couponId") as string);
    const cart = await getOrCreateCart();

    if (!cart) {
      redirect("/cart?couponError=Could+not+find+your+cart");
    }

    await cartService.removeCoupon(cart.id, couponId);
    revalidatePath("/cart");
  } catch (error) {
    console.error("Error removing coupon:", error);
    redirect("/cart?couponError=Failed+to+remove+coupon");
  }
}

export async function proceedToCheckout() {
  const cart = await getOrCreateCart();

  if (!cart.cartItems || cart.cartItems.length === 0) {
    throw new Error("Cart is empty");
  }

  for (const item of cart.cartItems) {
    const product = await productService.getById(item.productId);
    if (!product || !product.isActive) {
      throw new Error(
        `Product ${product?.name || item.productId} is not available`
      );
    }

    if (product.stockQuantity < product.reservedStock + item.quantity) {
      throw new Error(`Insufficient stock for ${product.name}`);
    }
  }

  redirect("/checkout");
}