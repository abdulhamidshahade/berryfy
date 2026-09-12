import Link from 'next/link';
import { ProductService } from '../lib/services/product/service';
import { CategoryService } from '../lib/services/category/service';
import { getCurrentUser } from '../lib/actions/auth-actions';
import { formatCurrency } from '../lib/utils/formatCurrency';
import { ProductDto } from '../types/product';
import { CategoryDto } from '../types/category';
import './home.css';

function HeroSection() {
  return (
    <section className="bg-primary text-white py-5">
      <div className="container">
        <div className="row align-items-center min-vh-50">
          <div className="col-lg-6">
            <div className="hero-content">
              <h1 className="display-4 fw-bold mb-4">
                Welcome to <span className="text-warning">Berryfy</span>
              </h1>
              <p className="lead mb-4">
                Discover amazing products with unbeatable prices. Your one-stop destination 
                for quality shopping with fast delivery and excellent customer service.
              </p>
              <div className="d-flex gap-3 flex-wrap">
                <Link href="/products" className="btn btn-warning btn-lg px-4">
                  <i className="bi bi-shop me-2"></i>
                  Shop Now
                </Link>
                <Link href="/categories" className="btn btn-outline-light btn-lg px-4">
                  <i className="bi bi-grid me-2"></i>
                  Browse Categories
                </Link>
              </div>
            </div>
          </div>
          <div className="col-lg-6 text-center">
            <div className="hero-image mt-4 mt-lg-0">
              <i className="bi bi-bag-heart display-1 text-warning opacity-75"></i>
                <div className="mt-3">
                  <div className="d-flex justify-content-center align-items-center gap-2 flex-wrap">
                    <div className="badge bg-warning text-dark fs-6 px-3 py-2">
                      <i className="bi bi-truck me-1"></i>
                      Free Shipping
                    </div>
                    <div className="badge bg-warning text-dark fs-6 px-3 py-2">
                      <i className="bi bi-shield-check me-1"></i>
                      Secure Payment
                    </div>
                    <div className="badge bg-warning text-dark fs-6 px-3 py-2">
                      <i className="bi bi-arrow-clockwise me-1"></i>
                      Easy Returns
                    </div>
                  </div>
                </div>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}

function FeaturesSection() {
  const features = [
    {
      icon: 'bi-lightning-charge',
      title: 'Fast Delivery',
      description: 'Get your orders delivered quickly with our express shipping options.',
      color: 'text-warning'
    },
    {
      icon: 'bi-shield-check',
      title: 'Secure Shopping',
      description: 'Shop with confidence using our secure payment gateway and data protection.',
      color: 'text-success'
    },
    {
      icon: 'bi-headset',
      title: '24/7 Support',
      description: 'Our customer support team is always ready to help you with any questions.',
      color: 'text-info'
    },
    {
      icon: 'bi-arrow-clockwise',
      title: 'Easy Returns',
      description: 'Not satisfied? Return your items hassle-free within 30 days.',
      color: 'text-danger'
    }
  ];

  return (
    <section className="py-5 bg-light">
      <div className="container">
        <div className="row">
          <div className="col-12 text-center mb-5">
            <h2 className="display-5 fw-bold text-dark">Why Choose Berryfy?</h2>
            <p className="lead text-muted">Experience the best online shopping with our premium features</p>
          </div>
        </div>
        <div className="row g-4">
          {features.map((feature, index) => (
            <div key={index} className="col-md-6 col-lg-3">
              <div className="card h-100 border-0 shadow-sm hover-lift">
                <div className="card-body text-center p-4">
                  <div className={`display-4 ${feature.color} mb-3`}>
                    <i className={feature.icon}></i>
                  </div>
                  <h5 className="card-title fw-bold">{feature.title}</h5>
                  <p className="card-text text-muted">{feature.description}</p>
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}

async function CategoriesSection() {
  let categories: CategoryDto[] = [];
  
  try {
    const categoryService = new CategoryService();
    const allCategories = await categoryService.getAll();
    categories = allCategories.slice(0, 6);
  } catch (error) {
    console.error('Failed to fetch categories:', error);
  }

  return (
    <section className="py-5">
      <div className="container">
        <div className="row">
          <div className="col-12 text-center mb-5">
            <h2 className="display-5 fw-bold text-dark">Shop by Category</h2>
            <p className="lead text-muted">Explore our wide range of product categories</p>
          </div>
        </div>
        <div className="row g-4">
          {categories.length > 0 ? (
            categories.map((category) => (
              <div key={category.id} className="col-md-6 col-lg-4">
                <Link href={`/categories/${category.id}`} className="text-decoration-none">
                  <div className="card border-0 shadow-sm hover-lift h-100">
                    <div className="card-img-top bg-primary bg-opacity-10 d-flex align-items-center justify-content-center" style={{ height: '200px' }}>
                      {category.imageUrl ? (
                        <img 
                          src={category.imageUrl} 
                          alt={category.name}
                          className="img-fluid rounded"
                          style={{ maxHeight: '150px', objectFit: 'cover' }}
                        />
                      ) : (
                        <i className="bi bi-grid display-1 text-primary opacity-50"></i>
                      )}
                    </div>
                    <div className="card-body text-center">
                      <h5 className="card-title fw-bold text-dark">{category.name}</h5>
                      <p className="card-text text-muted small">{category.description}</p>
                      <div className="btn btn-outline-primary btn-sm">
                        <i className="bi bi-arrow-right me-1"></i>
                        Explore
                      </div>
                    </div>
                  </div>
                </Link>
              </div>
            ))
          ) : (
            <div className="col-12 text-center">
              <div className="py-5">
                <i className="bi bi-grid display-1 text-muted opacity-50"></i>
                <h4 className="text-muted mt-3">Categories Coming Soon</h4>
                <p className="text-muted">We&apos;re working on adding amazing product categories for you!</p>
              </div>
            </div>
          )}
        </div>
        {categories.length > 0 && (
          <div className="row mt-4">
            <div className="col-12 text-center">
              <Link href="/categories" className="btn btn-primary btn-lg">
                <i className="bi bi-grid me-2"></i>
                View All Categories
              </Link>
            </div>
          </div>
        )}
      </div>
    </section>
  );
}


async function FeaturedProductsSection() {
  let products: ProductDto[] = [];
  
  try {
    const productService = new ProductService();
    const paginatedProducts = await productService.getPaginated({
      pageNumber: 1,
      pageSize: 50, 
      isActive: true
    });
    products = paginatedProducts.data.slice(0, 8);
  } catch (error) {
    console.error('Failed to fetch products:', error);
  }

  return (
    <section className="py-5 bg-light">
      <div className="container">
        <div className="row">
          <div className="col-12 text-center mb-5">
            <h2 className="display-5 fw-bold text-dark">Featured Products</h2>
            <p className="lead text-muted">Discover our most popular and trending items</p>
          </div>
        </div>
        <div className="row g-4">
          {products.length > 0 ? (
            products.map((product) => {
              const stockStatus =
                product.stockQuantity <= product.lowStockThreshold
                  ? 'danger'
                  : product.stockQuantity <= product.lowStockThreshold * 2
                  ? 'warning'
                  : 'success';
              const stockIcon =
                stockStatus === 'danger'
                  ? 'bi-exclamation-triangle-fill'
                  : stockStatus === 'warning'
                  ? 'bi-exclamation-circle-fill'
                  : 'bi-check-circle-fill';

              return (
                <div key={product.id} className="col-md-6 col-lg-3">
                  <div className="card border-0 shadow-sm hover-lift h-100 position-relative">
                    <div className="position-relative">
                      <div className="card-img-top bg-white d-flex align-items-center justify-content-center" style={{ height: '250px' }}>
                        {product.imageUrl ? (
                          <img 
                            src={product.imageUrl} 
                            alt={product.name}
                            className="img-fluid"
                            style={{ maxHeight: '200px', objectFit: 'cover' }}
                          />
                        ) : (
                          <i className="bi bi-box display-1 text-muted opacity-50"></i>
                        )}
                      </div>
                    </div>
                    <div className="card-body d-flex flex-column">
                      <Link
                        href={`/products/${product.id}`}
                        className="stretched-link"
                        aria-label={`View ${product.name}`}
                      />
                      <h6 className="card-title fw-bold text-truncate" title={product.name}>
                        {product.name}
                      </h6>
                      <p className="card-text text-muted small flex-grow-1 text-truncate-2">
                        {product.description}
                      </p>
                      <div className="mt-auto">
                        <div className="d-flex justify-content-between align-items-center mb-2">
                          <span className="h5 text-primary fw-bold mb-0">
                            {formatCurrency(product.price)}
                          </span>
                          <span className={`badge bg-${stockStatus}`}>
                            <i className={`bi ${stockIcon} me-1`}></i>
                            {stockStatus === 'success'
                              ? 'In Stock'
                              : `Only ${product.stockQuantity} left`}
                          </span>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              );
            })
          ) : (
            <div className="col-12 text-center">
              <div className="py-5">
                <i className="bi bi-box-seam display-1 text-muted opacity-50"></i>
                <h4 className="text-muted mt-3">Products Coming Soon</h4>
                <p className="text-muted">We&apos;re adding amazing products to our catalog!</p>
              </div>
            </div>
          )}
        </div>
        {products.length > 0 && (
          <div className="row mt-4">
            <div className="col-12 text-center">
              <Link href="/products" className="btn btn-primary btn-lg">
                <i className="bi bi-shop me-2"></i>
                View All Products
              </Link>
            </div>
          </div>
        )}
      </div>
    </section>
  );
}

function TestimonialsSection() {
  const testimonials = [
    {
      name: "Sarah Johnson",
      role: "Verified Customer",
      content: "Amazing shopping experience! Fast delivery and excellent customer service. Highly recommended!",
      rating: 5
    },
    {
      name: "Mike Chen",
      role: "Regular Customer",
      content: "Great product quality and competitive prices. Berryfy has become my go-to online store.",
      rating: 5
    },
    {
      name: "Emily Davis",
      role: "Happy Customer",
      content: "Love the user-friendly website and the wide variety of products. Shopping here is always a pleasure!",
      rating: 5
    }
  ];

  return (
    <section className="py-5">
      <div className="container">
        <div className="row">
          <div className="col-12 text-center mb-5">
            <h2 className="display-5 fw-bold text-dark">What Our Customers Say</h2>
            <p className="lead text-muted">Don&apos;t just take our word for it - hear from our satisfied customers</p>
          </div>
        </div>
        <div className="row g-4">
          {testimonials.map((testimonial, index) => (
            <div key={index} className="col-md-4">
              <div className="card border-0 shadow-sm h-100">
                <div className="card-body text-center p-4">
                  <div className="mb-3">
                    {[...Array(testimonial.rating)].map((_, i) => (
                      <i key={i} className="bi bi-star-fill text-warning"></i>
                    ))}
                  </div>
                  <p className="card-text text-muted mb-4">&quot;{testimonial.content}&quot;</p>
                  <div className="mt-auto">
                    <h6 className="fw-bold mb-0">{testimonial.name}</h6>
                    <small className="text-muted">{testimonial.role}</small>
                  </div>
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}


function NewsletterSection() {
  return (
    <section className="py-5 bg-primary text-white">
      <div className="container">
        <div className="row justify-content-center">
          <div className="col-lg-8 text-center">
            <h2 className="display-5 fw-bold mb-4">Stay Updated</h2>
            <p className="lead mb-4">
              Subscribe to our newsletter and be the first to know about new products, 
              exclusive deals, and special offers!
            </p>
            <form className="row g-3 justify-content-center">
              <div className="col-md-6">
                <div className="input-group input-group-lg">
                  <input 
                    type="email" 
                    className="form-control" 
                    placeholder="Enter your email address"
                    required
                  />
                  <button className="btn btn-warning" type="submit">
                    <i className="bi bi-envelope me-1"></i>
                    Subscribe
                  </button>
                </div>
              </div>
            </form>
            <small className="text-light opacity-75 mt-2 d-block">
              We respect your privacy. Unsubscribe at any time.
            </small>
          </div>
        </div>
      </div>
    </section>
  );
}

function StatsSection() {
  const stats = [
    { number: "10,000+", label: "Happy Customers", icon: "bi-people" },
    { number: "5,000+", label: "Products", icon: "bi-box-seam" },
    { number: "50+", label: "Categories", icon: "bi-grid" },
    { number: "99.9%", label: "Satisfaction Rate", icon: "bi-heart" }
  ];

  return (
    <section className="py-5 bg-dark text-white">
      <div className="container">
        <div className="row g-4">
          {stats.map((stat, index) => (
            <div key={index} className="col-md-6 col-lg-3 text-center">
              <div className="mb-3">
                <i className={`${stat.icon} display-4 text-warning`}></i>
              </div>
              <h3 className="display-6 fw-bold text-warning">{stat.number}</h3>
              <p className="text-light">{stat.label}</p>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}

interface HomePageProps {
  searchParams: Promise<{
    error?: string;
    message?: string;
  }>;
}

export default async function HomePage({ searchParams }: HomePageProps) {
  const user = await getCurrentUser();
  const params = await searchParams;
  const { error, message } = params;

  return (
    <>
      {error && (
        <div className="container mt-3">
          <div className="alert alert-danger alert-dismissible fade show" role="alert">
            <i className="bi bi-exclamation-triangle me-2"></i>
            {decodeURIComponent(error)}
            <button type="button" className="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
          </div>
        </div>
      )}
      
      {message && (
        <div className="container mt-3">
          <div className="alert alert-success alert-dismissible fade show" role="alert">
            <i className="bi bi-check-circle me-2"></i>
            {decodeURIComponent(message)}
            <button type="button" className="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
          </div>
        </div>
      )}

      <HeroSection />
      <FeaturesSection />
      <CategoriesSection />
      <FeaturedProductsSection />
      <TestimonialsSection />
      <StatsSection />
      <NewsletterSection />
    </>
  );
}

export const metadata = {
  title: 'Berryfy - Your Shopping Destination',
  description: 'Discover amazing products with unbeatable prices. Your one-stop destination for quality shopping with fast delivery and excellent customer service.',
};
