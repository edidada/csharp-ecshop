-- csharp-ecshop core schema; MySQL 8.0+, InnoDB, utf8mb4.
-- All monetary values are DECIMAL(12,2). Timestamps use UTC.
SET NAMES utf8mb4;
SET time_zone = '+00:00';

CREATE TABLE region (
  region_id BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
  parent_id BIGINT UNSIGNED NOT NULL DEFAULT 0,
  region_name VARCHAR(120) NOT NULL,
  region_type TINYINT UNSIGNED NOT NULL,
  KEY idx_region_parent (parent_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE users (
  user_id BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
  user_name VARCHAR(60) NOT NULL,
  email VARCHAR(120) NOT NULL,
  password_hash VARCHAR(255) NOT NULL,
  mobile_phone VARCHAR(32) NOT NULL DEFAULT '',
  user_money DECIMAL(12,2) NOT NULL DEFAULT 0.00,
  pay_points INT NOT NULL DEFAULT 0,
  rank_points INT NOT NULL DEFAULT 0,
  is_validated TINYINT(1) NOT NULL DEFAULT 0,
  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  UNIQUE KEY uk_users_name (user_name),
  UNIQUE KEY uk_users_email (email)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE sessions (
  token_hash CHAR(64) PRIMARY KEY,
  user_id BIGINT UNSIGNED NOT NULL,
  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  KEY idx_sessions_user (user_id),
  CONSTRAINT fk_sessions_user FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE user_address (
  address_id BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
  user_id BIGINT UNSIGNED NOT NULL,
  consignee VARCHAR(60) NOT NULL,
  country_id BIGINT UNSIGNED NOT NULL DEFAULT 0,
  province_id BIGINT UNSIGNED NOT NULL DEFAULT 0,
  city_id BIGINT UNSIGNED NOT NULL DEFAULT 0,
  district_id BIGINT UNSIGNED NOT NULL DEFAULT 0,
  address VARCHAR(255) NOT NULL,
  zipcode VARCHAR(20) NOT NULL DEFAULT '',
  mobile VARCHAR(32) NOT NULL DEFAULT '',
  is_default TINYINT(1) NOT NULL DEFAULT 0,
  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  KEY idx_address_user (user_id),
  CONSTRAINT fk_address_user FOREIGN KEY (user_id) REFERENCES users(user_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE category (
  cat_id BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
  parent_id BIGINT UNSIGNED NOT NULL DEFAULT 0,
  cat_name VARCHAR(120) NOT NULL,
  sort_order INT NOT NULL DEFAULT 50,
  is_show TINYINT(1) NOT NULL DEFAULT 1,
  keywords VARCHAR(255) NOT NULL DEFAULT '',
  cat_desc VARCHAR(255) NOT NULL DEFAULT '',
  KEY idx_category_parent_sort (parent_id, sort_order)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE brand (
  brand_id BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
  brand_name VARCHAR(120) NOT NULL,
  brand_logo VARCHAR(255) NOT NULL DEFAULT '',
  site_url VARCHAR(255) NOT NULL DEFAULT '',
  is_show TINYINT(1) NOT NULL DEFAULT 1,
  sort_order INT NOT NULL DEFAULT 50,
  UNIQUE KEY uk_brand_name (brand_name)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE article_cat (
  cat_id BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
  cat_name VARCHAR(120) NOT NULL,
  cat_desc TEXT NOT NULL,
  keywords VARCHAR(255) NOT NULL DEFAULT '',
  sort_order INT NOT NULL DEFAULT 50,
  is_show TINYINT(1) NOT NULL DEFAULT 1
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE article (
  article_id BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
  cat_id BIGINT UNSIGNED NOT NULL,
  title VARCHAR(255) NOT NULL,
  author VARCHAR(255) NOT NULL DEFAULT '',
  article_desc TEXT NOT NULL,
  content LONGTEXT NOT NULL,
  keywords VARCHAR(255) NOT NULL DEFAULT '',
  is_open TINYINT(1) NOT NULL DEFAULT 1,
  KEY idx_article_category_open (cat_id, is_open, article_id),
  CONSTRAINT fk_article_category FOREIGN KEY (cat_id) REFERENCES article_cat(cat_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE goods (
  goods_id BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
  cat_id BIGINT UNSIGNED NOT NULL,
  brand_id BIGINT UNSIGNED NOT NULL DEFAULT 0,
  goods_sn VARCHAR(80) NOT NULL DEFAULT '',
  goods_name VARCHAR(255) NOT NULL,
  goods_brief VARCHAR(255) NOT NULL DEFAULT '',
  keywords VARCHAR(255) NOT NULL DEFAULT '',
  goods_desc LONGTEXT NOT NULL,
  shop_price DECIMAL(12,2) NOT NULL,
  market_price DECIMAL(12,2) NOT NULL DEFAULT 0.00,
  goods_number INT NOT NULL DEFAULT 0,
  is_on_sale TINYINT(1) NOT NULL DEFAULT 1,
  is_alone_sale TINYINT(1) NOT NULL DEFAULT 1,
  is_delete TINYINT(1) NOT NULL DEFAULT 0,
  add_time DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  last_update DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  UNIQUE KEY uk_goods_sn (goods_sn),
  KEY idx_goods_listing (cat_id, is_on_sale, is_delete),
  KEY idx_goods_brand (brand_id),
  CONSTRAINT fk_goods_category FOREIGN KEY (cat_id) REFERENCES category(cat_id),
  CONSTRAINT fk_goods_brand FOREIGN KEY (brand_id) REFERENCES brand(brand_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE products (
  product_id BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
  goods_id BIGINT UNSIGNED NOT NULL,
  product_sn VARCHAR(80) NOT NULL DEFAULT '',
  product_number INT NOT NULL DEFAULT 0,
  product_attr TEXT NOT NULL,
  UNIQUE KEY uk_product_sn (product_sn),
  KEY idx_product_goods (goods_id),
  CONSTRAINT fk_product_goods FOREIGN KEY (goods_id) REFERENCES goods(goods_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE goods_attr (
  goods_attr_id BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
  goods_id BIGINT UNSIGNED NOT NULL,
  attr_id BIGINT UNSIGNED NOT NULL DEFAULT 0,
  attr_value TEXT NOT NULL,
  attr_price DECIMAL(12,2) NOT NULL DEFAULT 0.00,
  KEY idx_goods_attr_goods (goods_id),
  CONSTRAINT fk_goods_attr_goods FOREIGN KEY (goods_id) REFERENCES goods(goods_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE goods_gallery (
  img_id BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
  goods_id BIGINT UNSIGNED NOT NULL,
  img_url VARCHAR(255) NOT NULL,
  img_desc VARCHAR(255) NOT NULL DEFAULT '',
  sort_order INT NOT NULL DEFAULT 0,
  KEY idx_gallery_goods (goods_id),
  CONSTRAINT fk_gallery_goods FOREIGN KEY (goods_id) REFERENCES goods(goods_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE cart (
  rec_id BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
  user_id BIGINT UNSIGNED NULL,
  session_key CHAR(64) NULL,
  goods_id BIGINT UNSIGNED NOT NULL,
  product_id BIGINT UNSIGNED NULL,
  attr_signature CHAR(64) NOT NULL DEFAULT '',
  goods_attr TEXT NOT NULL,
  goods_number INT NOT NULL,
  version INT NOT NULL DEFAULT 1,
  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  KEY idx_cart_user (user_id), KEY idx_cart_session (session_key),
  -- The HTTP cart currently represents the no-product/no-attribute variant.
  -- This key makes its increment operation atomic; future product variants
  -- should use a non-null product identity in their own cart key.
  UNIQUE KEY uk_cart_user_goods_base (user_id, goods_id, attr_signature),
  CONSTRAINT fk_cart_user FOREIGN KEY (user_id) REFERENCES users(user_id),
  CONSTRAINT fk_cart_goods FOREIGN KEY (goods_id) REFERENCES goods(goods_id),
  CONSTRAINT fk_cart_product FOREIGN KEY (product_id) REFERENCES products(product_id),
  CONSTRAINT ck_cart_owner CHECK (user_id IS NOT NULL OR session_key IS NOT NULL),
  CONSTRAINT ck_cart_quantity CHECK (goods_number > 0)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE payment (
  pay_id BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
  pay_name VARCHAR(120) NOT NULL,
  pay_code VARCHAR(60) NOT NULL,
  enabled TINYINT(1) NOT NULL DEFAULT 1,
  pay_fee DECIMAL(12,2) NOT NULL DEFAULT 0.00,
  UNIQUE KEY uk_payment_code (pay_code)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE shipping (
  shipping_id BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
  shipping_name VARCHAR(120) NOT NULL,
  shipping_code VARCHAR(60) NOT NULL,
  enabled TINYINT(1) NOT NULL DEFAULT 1,
  shipping_fee DECIMAL(12,2) NOT NULL DEFAULT 0.00,
  UNIQUE KEY uk_shipping_code (shipping_code)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE order_info (
  order_id BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
  order_sn VARCHAR(40) NOT NULL,
  user_id BIGINT UNSIGNED NOT NULL,
  order_status VARCHAR(32) NOT NULL DEFAULT 'pending_payment',
  shipping_status VARCHAR(32) NOT NULL DEFAULT 'unshipped',
  pay_status VARCHAR(32) NOT NULL DEFAULT 'unpaid',
  consignee VARCHAR(60) NOT NULL, address VARCHAR(255) NOT NULL,
  mobile VARCHAR(32) NOT NULL DEFAULT '',
  shipping_id BIGINT UNSIGNED NULL, pay_id BIGINT UNSIGNED NULL,
  goods_amount DECIMAL(12,2) NOT NULL, shipping_fee DECIMAL(12,2) NOT NULL DEFAULT 0.00, payment_fee DECIMAL(12,2) NOT NULL DEFAULT 0.00,
  discount_amount DECIMAL(12,2) NOT NULL DEFAULT 0.00, order_amount DECIMAL(12,2) NOT NULL,
  idempotency_key VARCHAR(100) NOT NULL,
  request_fingerprint VARCHAR(512) NOT NULL,
  remark VARCHAR(255) NOT NULL DEFAULT '',
  add_time DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  pay_time DATETIME NULL, shipping_time DATETIME NULL,
  UNIQUE KEY uk_order_sn (order_sn), UNIQUE KEY uk_order_idempotency (user_id, idempotency_key),
  KEY idx_order_user_time (user_id, add_time),
  CONSTRAINT fk_order_user FOREIGN KEY (user_id) REFERENCES users(user_id),
  CONSTRAINT fk_order_shipping FOREIGN KEY (shipping_id) REFERENCES shipping(shipping_id),
  CONSTRAINT fk_order_payment FOREIGN KEY (pay_id) REFERENCES payment(pay_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE order_goods (
  rec_id BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
  order_id BIGINT UNSIGNED NOT NULL, goods_id BIGINT UNSIGNED NOT NULL,
  product_id BIGINT UNSIGNED NULL, goods_name VARCHAR(255) NOT NULL,
  goods_sn VARCHAR(80) NOT NULL DEFAULT '', goods_number INT NOT NULL,
  market_price DECIMAL(12,2) NOT NULL, goods_price DECIMAL(12,2) NOT NULL,
  goods_attr TEXT NOT NULL, KEY idx_order_goods_order (order_id),
  CONSTRAINT fk_order_goods_order FOREIGN KEY (order_id) REFERENCES order_info(order_id),
  CONSTRAINT ck_order_goods_quantity CHECK (goods_number > 0)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE order_action (
  action_id BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
  order_id BIGINT UNSIGNED NOT NULL, actor_type VARCHAR(20) NOT NULL,
  actor_id BIGINT UNSIGNED NULL, action_note VARCHAR(255) NOT NULL DEFAULT '',
  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  KEY idx_order_action_order (order_id),
  CONSTRAINT fk_action_order FOREIGN KEY (order_id) REFERENCES order_info(order_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE pay_log (
  log_id BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
  order_id BIGINT UNSIGNED NOT NULL, provider VARCHAR(60) NOT NULL,
  provider_trade_no VARCHAR(120) NOT NULL, amount DECIMAL(12,2) NOT NULL,
  status VARCHAR(32) NOT NULL, raw_payload JSON NULL,
  received_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE KEY uk_provider_trade (provider, provider_trade_no),
  CONSTRAINT fk_paylog_order FOREIGN KEY (order_id) REFERENCES order_info(order_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE comment (
  comment_id BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
  comment_type TINYINT UNSIGNED NOT NULL DEFAULT 0,
  id_value BIGINT UNSIGNED NOT NULL,
  user_id BIGINT UNSIGNED NOT NULL,
  user_name VARCHAR(60) NOT NULL,
  content TEXT NOT NULL,
  status TINYINT(1) NOT NULL DEFAULT 1,
  add_time DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  KEY idx_comment_goods_status (id_value, status, comment_id),
  CONSTRAINT fk_comment_goods FOREIGN KEY (id_value) REFERENCES goods(goods_id),
  CONSTRAINT fk_comment_user FOREIGN KEY (user_id) REFERENCES users(user_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE goods_activity (
  act_id BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT, act_name VARCHAR(255) NOT NULL, act_desc TEXT NOT NULL,
  act_type TINYINT UNSIGNED NOT NULL, goods_id BIGINT UNSIGNED NOT NULL, product_id BIGINT UNSIGNED NOT NULL DEFAULT 0,
  goods_name VARCHAR(255) NOT NULL DEFAULT '', start_time BIGINT UNSIGNED NOT NULL, end_time BIGINT UNSIGNED NOT NULL,
  is_finished TINYINT(1) NOT NULL DEFAULT 0, ext_info TEXT NOT NULL,
  KEY idx_activity_active (act_type,start_time,end_time,is_finished), CONSTRAINT fk_activity_goods FOREIGN KEY(goods_id) REFERENCES goods(goods_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
