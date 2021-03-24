-- MariaDB dump 10.17  Distrib 10.5.6-MariaDB, for osx10.15 (x86_64)
--
-- Host: xolotl.mictlanix.net    Database: mbe_ramos
-- ------------------------------------------------------
-- Server version	10.5.9-MariaDB-log

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Table structure for table `access_privilege`
--

DROP TABLE IF EXISTS `access_privilege`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `access_privilege` (
  `access_privilege_id` int(11) NOT NULL AUTO_INCREMENT,
  `user` varchar(20) COLLATE utf8_unicode_ci NOT NULL,
  `object` int(11) NOT NULL,
  `privileges` int(11) NOT NULL,
  PRIMARY KEY (`access_privilege_id`),
  KEY `user_access_privilege_idx` (`user`),
  CONSTRAINT `user_access_privilege` FOREIGN KEY (`user`) REFERENCES `user` (`user_id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `address`
--

DROP TABLE IF EXISTS `address`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `address` (
  `address_id` int(11) NOT NULL AUTO_INCREMENT,
  `type` int(11) NOT NULL,
  `street` varchar(150) COLLATE utf8_unicode_ci NOT NULL,
  `exterior_number` varchar(25) COLLATE utf8_unicode_ci NOT NULL,
  `interior_number` varchar(25) COLLATE utf8_unicode_ci DEFAULT NULL,
  `postal_code` varchar(5) COLLATE utf8_unicode_ci NOT NULL,
  `neighborhood` varchar(100) COLLATE utf8_unicode_ci NOT NULL,
  `locality` varchar(100) COLLATE utf8_unicode_ci DEFAULT NULL,
  `borough` varchar(50) COLLATE utf8_unicode_ci NOT NULL,
  `state` varchar(50) COLLATE utf8_unicode_ci NOT NULL,
  `city` varchar(50) COLLATE utf8_unicode_ci DEFAULT NULL,
  `country` varchar(50) COLLATE utf8_unicode_ci NOT NULL,
  `comment` varchar(500) COLLATE utf8_unicode_ci DEFAULT NULL,
  PRIMARY KEY (`address_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `bank_account`
--

DROP TABLE IF EXISTS `bank_account`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `bank_account` (
  `bank_account_id` int(11) NOT NULL AUTO_INCREMENT,
  `bank_name` varchar(250) COLLATE utf8_unicode_ci NOT NULL,
  `account_number` varchar(20) COLLATE utf8_unicode_ci NOT NULL,
  `reference` varchar(20) COLLATE utf8_unicode_ci DEFAULT NULL,
  `routing_number` varchar(18) COLLATE utf8_unicode_ci DEFAULT NULL,
  `comment` varchar(500) COLLATE utf8_unicode_ci DEFAULT NULL,
  PRIMARY KEY (`bank_account_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `cash_count`
--

DROP TABLE IF EXISTS `cash_count`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `cash_count` (
  `cash_count_id` int(11) NOT NULL AUTO_INCREMENT,
  `session` int(11) NOT NULL,
  `denomination` decimal(18,4) NOT NULL,
  `quantity` int(11) NOT NULL,
  `type` int(11) NOT NULL,
  PRIMARY KEY (`cash_count_id`),
  KEY `cash_count_cash_session_fk_idx` (`session`),
  CONSTRAINT `cash_count_cash_session_fk` FOREIGN KEY (`session`) REFERENCES `cash_session` (`cash_session_id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `cash_drawer`
--

DROP TABLE IF EXISTS `cash_drawer`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `cash_drawer` (
  `cash_drawer_id` int(11) NOT NULL AUTO_INCREMENT,
  `store` int(11) NOT NULL DEFAULT 1,
  `code` varchar(25) COLLATE utf8_unicode_ci NOT NULL,
  `name` varchar(250) COLLATE utf8_unicode_ci NOT NULL,
  `comment` varchar(500) COLLATE utf8_unicode_ci DEFAULT NULL,
  PRIMARY KEY (`cash_drawer_id`),
  UNIQUE KEY `code_UNIQUE` (`code`),
  KEY `cash_drawer_store_fk_idx` (`store`),
  CONSTRAINT `cash_drawer_store_fk` FOREIGN KEY (`store`) REFERENCES `store` (`store_id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `cash_session`
--

DROP TABLE IF EXISTS `cash_session`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `cash_session` (
  `cash_session_id` int(11) NOT NULL AUTO_INCREMENT,
  `start` datetime NOT NULL,
  `end` datetime DEFAULT NULL,
  `cashier` int(11) NOT NULL,
  `cash_drawer` int(11) NOT NULL,
  PRIMARY KEY (`cash_session_id`),
  KEY `cash_session_employed_fk_idx` (`cashier`),
  KEY `cash_session_cash_drawer_fk_idx` (`cash_drawer`),
  CONSTRAINT `cash_session_cash_drawer_fk` FOREIGN KEY (`cash_drawer`) REFERENCES `cash_drawer` (`cash_drawer_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `cash_session_employed_fk` FOREIGN KEY (`cashier`) REFERENCES `employee` (`employee_id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `contact`
--

DROP TABLE IF EXISTS `contact`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `contact` (
  `contact_id` int(11) NOT NULL AUTO_INCREMENT,
  `name` varchar(250) COLLATE utf8_unicode_ci NOT NULL,
  `job_title` varchar(100) COLLATE utf8_unicode_ci DEFAULT NULL,
  `phone` varchar(25) COLLATE utf8_unicode_ci NOT NULL,
  `phone_ext` varchar(5) COLLATE utf8_unicode_ci DEFAULT NULL,
  `mobile` varchar(25) COLLATE utf8_unicode_ci DEFAULT NULL,
  `fax` varchar(25) COLLATE utf8_unicode_ci DEFAULT NULL,
  `website` varchar(80) COLLATE utf8_unicode_ci DEFAULT NULL,
  `email` varchar(80) COLLATE utf8_unicode_ci NOT NULL,
  `im` varchar(80) COLLATE utf8_unicode_ci DEFAULT NULL,
  `sip` varchar(80) COLLATE utf8_unicode_ci DEFAULT NULL,
  `birthday` date DEFAULT NULL,
  `comment` varchar(500) COLLATE utf8_unicode_ci DEFAULT NULL,
  PRIMARY KEY (`contact_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `customer`
--

DROP TABLE IF EXISTS `customer`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `customer` (
  `customer_id` int(11) NOT NULL AUTO_INCREMENT,
  `code` varchar(25) COLLATE utf8_unicode_ci NOT NULL,
  `name` varchar(250) COLLATE utf8_unicode_ci NOT NULL,
  `zone` varchar(250) COLLATE utf8_unicode_ci DEFAULT NULL,
  `credit_limit` decimal(18,4) NOT NULL,
  `credit_days` int(11) NOT NULL,
  `comment` varchar(1024) COLLATE utf8_unicode_ci DEFAULT NULL,
  `price_list` int(11) NOT NULL,
  `shipping` tinyint(1) NOT NULL,
  `shipping_required_document` tinyint(1) NOT NULL,
  `salesperson` int(11) DEFAULT NULL,
  PRIMARY KEY (`customer_id`),
  KEY `price_list_id_idx` (`price_list`),
  CONSTRAINT `price_list_id` FOREIGN KEY (`price_list`) REFERENCES `price_list` (`price_list_id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `customer_address`
--

DROP TABLE IF EXISTS `customer_address`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `customer_address` (
  `customer` int(11) NOT NULL,
  `address` int(11) NOT NULL,
  PRIMARY KEY (`customer`,`address`),
  KEY `customer_address_customer_fk_idx` (`customer`),
  KEY `customer_address_address_fk_idx` (`address`),
  CONSTRAINT `customer_address_address_fk` FOREIGN KEY (`address`) REFERENCES `address` (`address_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `customer_address_customer_fk` FOREIGN KEY (`customer`) REFERENCES `customer` (`customer_id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `customer_contact`
--

DROP TABLE IF EXISTS `customer_contact`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `customer_contact` (
  `customer` int(11) NOT NULL,
  `contact` int(11) NOT NULL,
  PRIMARY KEY (`customer`,`contact`),
  KEY `customer_contact_customer_fk_idx` (`customer`),
  KEY `customer_contact_contact_fk_idx` (`contact`),
  CONSTRAINT `customer_contact_contact_fk` FOREIGN KEY (`contact`) REFERENCES `contact` (`contact_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `customer_contact_customer_fk` FOREIGN KEY (`customer`) REFERENCES `customer` (`customer_id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `customer_discount`
--

DROP TABLE IF EXISTS `customer_discount`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `customer_discount` (
  `customer_discount_id` int(11) NOT NULL AUTO_INCREMENT,
  `customer` int(11) NOT NULL,
  `product` int(11) NOT NULL,
  `discount` decimal(9,8) NOT NULL,
  PRIMARY KEY (`customer_discount_id`),
  UNIQUE KEY `customer_discount_unique_idx` (`customer`,`product`),
  KEY `customer_discount_customer_fk` (`customer`),
  KEY `customer_discount_product_fk` (`product`),
  CONSTRAINT `customer_discount_customer_fk` FOREIGN KEY (`customer`) REFERENCES `customer` (`customer_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `customer_discount_product_fk` FOREIGN KEY (`product`) REFERENCES `product` (`product_id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `customer_payment`
--

DROP TABLE IF EXISTS `customer_payment`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `customer_payment` (
  `customer_payment_id` int(11) NOT NULL AUTO_INCREMENT,
  `amount` decimal(18,4) NOT NULL,
  `method` int(11) NOT NULL,
  `commission` decimal(10,4) DEFAULT NULL,
  `payment_charge` int(11) DEFAULT NULL,
  `date` datetime NOT NULL,
  `cash_session` int(11) DEFAULT NULL,
  `reference` varchar(50) COLLATE utf8_unicode_ci DEFAULT NULL,
  `customer` int(11) NOT NULL,
  `store` int(11) NOT NULL,
  `serial` int(11) NOT NULL,
  `creator` int(11) NOT NULL,
  `updater` int(11) NOT NULL,
  `creation_time` datetime NOT NULL,
  `modification_time` datetime NOT NULL,
  `currency` int(11) NOT NULL,
  PRIMARY KEY (`customer_payment_id`),
  KEY `customer_payment_cash_session_fk_idx` (`cash_session`),
  KEY `customer_payment_customer_idx` (`customer`),
  KEY `customer_payment_store_fk_idx` (`store`),
  KEY `customer_payment_creator_fk_idx` (`creator`),
  KEY `customer_payment_updater_fk_idx` (`updater`),
  KEY `customer_payment_charge_fk` (`payment_charge`),
  CONSTRAINT `customer_payment_cash_session_fk` FOREIGN KEY (`cash_session`) REFERENCES `cash_session` (`cash_session_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `customer_payment_charge_fk` FOREIGN KEY (`payment_charge`) REFERENCES `payment_method_option` (`payment_method_option_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `customer_payment_creator_fk` FOREIGN KEY (`creator`) REFERENCES `employee` (`employee_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `customer_payment_customer` FOREIGN KEY (`customer`) REFERENCES `customer` (`customer_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `customer_payment_store_fk` FOREIGN KEY (`store`) REFERENCES `store` (`store_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `customer_payment_updater_fk` FOREIGN KEY (`updater`) REFERENCES `employee` (`employee_id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `customer_refund`
--

DROP TABLE IF EXISTS `customer_refund`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `customer_refund` (
  `customer_refund_id` int(11) NOT NULL AUTO_INCREMENT,
  `sales_order` int(11) NOT NULL,
  `customer` int(11) DEFAULT NULL,
  `creator` int(11) NOT NULL,
  `updater` int(11) NOT NULL,
  `sales_person` int(11) NOT NULL,
  `creation_time` datetime NOT NULL,
  `modification_time` datetime NOT NULL,
  `completed` tinyint(1) NOT NULL,
  `cancelled` tinyint(1) NOT NULL,
  `store` int(11) NOT NULL,
  `serial` int(11) NOT NULL,
  `date` datetime DEFAULT NULL,
  `currency` int(11) NOT NULL,
  `exchange_rate` decimal(8,4) NOT NULL DEFAULT 1.0000,
  PRIMARY KEY (`customer_refund_id`),
  KEY `customer_refund_sales_order_idx` (`sales_order`),
  KEY `customer_refund_creator_idx` (`creator`),
  KEY `customer_refund_updater_idx` (`updater`),
  KEY `customer_refund_sales_person_idx` (`sales_person`),
  KEY `customer_refund_customer_idx` (`customer`),
  KEY `customer_refund_store_idx` (`store`),
  CONSTRAINT `customer_refund_creator_fk` FOREIGN KEY (`creator`) REFERENCES `employee` (`employee_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `customer_refund_customer_fk` FOREIGN KEY (`customer`) REFERENCES `customer` (`customer_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `customer_refund_sales_order_fk` FOREIGN KEY (`sales_order`) REFERENCES `sales_order` (`sales_order_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `customer_refund_sales_person_fk` FOREIGN KEY (`sales_person`) REFERENCES `employee` (`employee_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `customer_refund_store_fk` FOREIGN KEY (`store`) REFERENCES `store` (`store_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `customer_refund_updater_fk` FOREIGN KEY (`updater`) REFERENCES `employee` (`employee_id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `customer_refund_detail`
--

DROP TABLE IF EXISTS `customer_refund_detail`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `customer_refund_detail` (
  `customer_refund_detail_id` int(11) NOT NULL AUTO_INCREMENT,
  `customer_refund` int(11) NOT NULL,
  `sales_order_detail` int(11) NOT NULL,
  `quantity` decimal(18,4) NOT NULL,
  `product` int(11) NOT NULL,
  `price` decimal(18,4) NOT NULL,
  `product_code` varchar(25) COLLATE utf8_unicode_ci NOT NULL,
  `product_name` varchar(250) COLLATE utf8_unicode_ci NOT NULL,
  `tax_rate` decimal(5,4) NOT NULL,
  `discount` decimal(9,8) NOT NULL,
  `exchange_rate` decimal(8,4) NOT NULL DEFAULT 1.0000,
  `currency` int(11) NOT NULL,
  `tax_included` tinyint(1) NOT NULL,
  PRIMARY KEY (`customer_refund_detail_id`),
  KEY `crd_sales_order_detail_idx` (`sales_order_detail`),
  KEY `crd_product_idx` (`product`),
  KEY `crd_customer_refund_idx` (`customer_refund`),
  CONSTRAINT `crd_customer_refund_fk` FOREIGN KEY (`customer_refund`) REFERENCES `customer_refund` (`customer_refund_id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `customer_taxpayer`
--

DROP TABLE IF EXISTS `customer_taxpayer`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `customer_taxpayer` (
  `customer` int(11) NOT NULL,
  `taxpayer` varchar(13) COLLATE utf8_unicode_ci NOT NULL,
  PRIMARY KEY (`customer`,`taxpayer`),
  KEY `customer_taxpayer_customer_idx` (`customer`),
  KEY `customer_taxpayer_taxpayer_idx` (`taxpayer`),
  CONSTRAINT `customer_taxpayer_customer_fk` FOREIGN KEY (`customer`) REFERENCES `customer` (`customer_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `customer_taxpayer_taxpayer_fk` FOREIGN KEY (`taxpayer`) REFERENCES `taxpayer_recipient` (`taxpayer_recipient_id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `delivery_order`
--

DROP TABLE IF EXISTS `delivery_order`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `delivery_order` (
  `delivery_order_id` int(11) NOT NULL AUTO_INCREMENT,
  `creator` int(11) NOT NULL,
  `updater` int(11) NOT NULL,
  `creation_time` datetime NOT NULL,
  `modification_time` datetime NOT NULL,
  `store` int(11) NOT NULL,
  `serial` int(11) NOT NULL,
  `customer` int(11) NOT NULL,
  `ship_to` int(11) DEFAULT NULL,
  `date` datetime DEFAULT NULL,
  `completed` tinyint(1) NOT NULL,
  `cancelled` tinyint(1) NOT NULL,
  `comment` varchar(500) COLLATE utf8_unicode_ci DEFAULT NULL,
  `delivered` tinyint(1) DEFAULT 0,
  PRIMARY KEY (`delivery_order_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `delivery_order_detail`
--

DROP TABLE IF EXISTS `delivery_order_detail`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `delivery_order_detail` (
  `delivery_order_detail_id` int(11) NOT NULL AUTO_INCREMENT,
  `delivery_order` int(11) NOT NULL,
  `sales_order_detail` int(11) DEFAULT NULL,
  `product` int(11) NOT NULL,
  `quantity` decimal(18,4) NOT NULL,
  `product_code` varchar(425) COLLATE utf8_unicode_ci NOT NULL,
  `product_name` varchar(250) COLLATE utf8_unicode_ci NOT NULL,
  PRIMARY KEY (`delivery_order_detail_id`),
  KEY `dod_delivery_order_idx` (`delivery_order`),
  KEY `dod_sales_order_detail_idx` (`sales_order_detail`),
  CONSTRAINT `dod_delivery_order_fk` FOREIGN KEY (`delivery_order`) REFERENCES `delivery_order` (`delivery_order_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `dod_sales_order_detail_fk` FOREIGN KEY (`sales_order_detail`) REFERENCES `sales_order_detail` (`sales_order_detail_id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `employee`
--

DROP TABLE IF EXISTS `employee`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `employee` (
  `employee_id` int(11) NOT NULL AUTO_INCREMENT,
  `first_name` varchar(100) COLLATE utf8_unicode_ci NOT NULL,
  `last_name` varchar(100) COLLATE utf8_unicode_ci NOT NULL,
  `nickname` varchar(50) COLLATE utf8_unicode_ci NOT NULL,
  `gender` tinyint(4) NOT NULL,
  `birthday` date NOT NULL,
  `taxpayer_id` varchar(13) COLLATE utf8_unicode_ci DEFAULT NULL,
  `sales_person` tinyint(1) NOT NULL,
  `active` tinyint(1) NOT NULL,
  `personal_id` varchar(18) COLLATE utf8_unicode_ci DEFAULT NULL,
  `start_job_date` date NOT NULL,
  `comment` varchar(500) COLLATE utf8_unicode_ci DEFAULT NULL,
  `enroll_number` int(11) DEFAULT NULL,
  PRIMARY KEY (`employee_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `exchange_rate`
--

DROP TABLE IF EXISTS `exchange_rate`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `exchange_rate` (
  `exchange_rate_id` int(11) NOT NULL AUTO_INCREMENT,
  `date` date NOT NULL,
  `rate` decimal(8,4) NOT NULL,
  `base` int(11) NOT NULL,
  `target` int(11) NOT NULL,
  PRIMARY KEY (`exchange_rate_id`),
  UNIQUE KEY `exchange_rate_daily_idx` (`date`,`base`,`target`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `expense_voucher`
--

DROP TABLE IF EXISTS `expense_voucher`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `expense_voucher` (
  `expense_voucher_id` int(11) NOT NULL AUTO_INCREMENT,
  `creator` int(11) NOT NULL DEFAULT 0,
  `updater` int(11) NOT NULL DEFAULT 0,
  `store` int(11) NOT NULL DEFAULT 0,
  `cash_session` int(11) NOT NULL DEFAULT 0,
  `comment` varchar(500) COLLATE utf8_unicode_ci DEFAULT NULL,
  `date` datetime NOT NULL DEFAULT current_timestamp(),
  `creation_time` datetime NOT NULL,
  `modification_time` datetime NOT NULL,
  `completed` tinyint(1) DEFAULT 0,
  `cancelled` tinyint(1) DEFAULT 0,
  PRIMARY KEY (`expense_voucher_id`),
  KEY `FK_expense_voucher_store` (`store`),
  KEY `FK_expense_voucher_employee` (`creator`),
  KEY `FK_expense_voucher_employee_2` (`updater`),
  KEY `FK_expense_voucher_cash_session` (`cash_session`),
  CONSTRAINT `FK_expense_voucher_cash_session` FOREIGN KEY (`cash_session`) REFERENCES `cash_session` (`cash_session_id`),
  CONSTRAINT `FK_expense_voucher_employee` FOREIGN KEY (`creator`) REFERENCES `employee` (`employee_id`),
  CONSTRAINT `FK_expense_voucher_employee_2` FOREIGN KEY (`updater`) REFERENCES `employee` (`employee_id`),
  CONSTRAINT `FK_expense_voucher_store` FOREIGN KEY (`store`) REFERENCES `store` (`store_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `expense_voucher_detail`
--

DROP TABLE IF EXISTS `expense_voucher_detail`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `expense_voucher_detail` (
  `expense_voucher_detail_id` int(11) NOT NULL AUTO_INCREMENT,
  `expense_voucher` int(11) NOT NULL DEFAULT 0,
  `expense` int(11) NOT NULL DEFAULT 0,
  `amount` decimal(18,2) NOT NULL,
  `comment` varchar(500) COLLATE utf8_unicode_ci DEFAULT NULL,
  PRIMARY KEY (`expense_voucher_detail_id`),
  KEY `FK_expense_voucher_detail_expense_voucher` (`expense_voucher`),
  KEY `FK_expense_voucher_detail_expenses` (`expense`),
  CONSTRAINT `FK_expense_voucher_detail_expense_voucher` FOREIGN KEY (`expense_voucher`) REFERENCES `expense_voucher` (`expense_voucher_id`),
  CONSTRAINT `FK_expense_voucher_detail_expenses` FOREIGN KEY (`expense`) REFERENCES `expenses` (`expense_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `expenses`
--

DROP TABLE IF EXISTS `expenses`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `expenses` (
  `expense_id` int(11) NOT NULL AUTO_INCREMENT,
  `expense` varchar(100) COLLATE utf8_unicode_ci NOT NULL DEFAULT '0',
  `comment` varchar(500) COLLATE utf8_unicode_ci DEFAULT '0',
  PRIMARY KEY (`expense_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `fiscal_document`
--

DROP TABLE IF EXISTS `fiscal_document`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `fiscal_document` (
  `fiscal_document_id` int(11) NOT NULL AUTO_INCREMENT,
  `creation_time` datetime NOT NULL,
  `modification_time` datetime NOT NULL,
  `creator` int(11) NOT NULL,
  `updater` int(11) NOT NULL,
  `issuer` varchar(13) COLLATE utf8_unicode_ci NOT NULL,
  `issuer_name` varchar(250) COLLATE utf8_unicode_ci DEFAULT NULL,
  `issuer_regime` varchar(3) COLLATE utf8_unicode_ci DEFAULT NULL,
  `issuer_regime_name` varchar(250) COLLATE utf8_unicode_ci DEFAULT NULL,
  `issuer_address` int(11) DEFAULT NULL,
  `customer` int(11) NOT NULL,
  `recipient` varchar(13) COLLATE utf8_unicode_ci NOT NULL,
  `recipient_name` varchar(250) COLLATE utf8_unicode_ci DEFAULT NULL,
  `recipient_address` int(11) DEFAULT NULL,
  `type` int(11) NOT NULL,
  `store` int(11) NOT NULL,
  `batch` varchar(10) COLLATE utf8_unicode_ci DEFAULT NULL,
  `serial` int(11) DEFAULT NULL,
  `issued` datetime DEFAULT NULL,
  `issued_at` int(11) DEFAULT NULL,
  `issued_location` varchar(250) COLLATE utf8_unicode_ci NOT NULL,
  `approval_number` int(11) DEFAULT NULL,
  `approval_year` int(11) DEFAULT NULL,
  `original_string` varchar(8000) COLLATE utf8_unicode_ci DEFAULT NULL,
  `issuer_digital_seal` varchar(8000) COLLATE utf8_unicode_ci DEFAULT NULL,
  `issuer_certificate_number` char(20) COLLATE utf8_unicode_ci DEFAULT NULL,
  `completed` bit(1) NOT NULL,
  `cancelled` bit(1) NOT NULL,
  `cancellation_date` datetime DEFAULT NULL,
  `reference` varchar(25) COLLATE utf8_unicode_ci DEFAULT NULL,
  `payment_method` int(11) NOT NULL,
  `payment_reference` varchar(50) COLLATE utf8_unicode_ci DEFAULT NULL,
  `exchange_rate` decimal(8,4) NOT NULL DEFAULT 1.0000,
  `currency` int(11) NOT NULL,
  `payment_terms` tinyint(4) NOT NULL,
  `usage` varchar(3) COLLATE utf8_unicode_ci DEFAULT NULL,
  `comment` varchar(1000) COLLATE utf8_unicode_ci DEFAULT NULL,
  `stamped` datetime DEFAULT NULL,
  `stamp_uuid` varchar(36) COLLATE utf8_unicode_ci DEFAULT NULL,
  `authority_digital_seal` varchar(500) CHARACTER SET utf8 DEFAULT NULL,
  `authority_certificate_number` varchar(20) COLLATE utf8_unicode_ci DEFAULT NULL,
  `version` decimal(3,1) NOT NULL,
  `provider` int(11) NOT NULL,
  `retention_rate` decimal(5,4) NOT NULL,
  `payment_date` date DEFAULT NULL,
  `payment_amount` decimal(18,2) DEFAULT NULL,
  PRIMARY KEY (`fiscal_document_id`),
  KEY `fiscal_document_creator_fk_idx` (`creator`),
  KEY `fiscal_document_updater_fk_idx` (`updater`),
  KEY `fiscal_document_customer_fk_idx` (`customer`),
  KEY `fiscal_document_taxpayer_fk_idx` (`issuer`),
  KEY `fiscal_document_recipient_address_fk_idx` (`recipient_address`),
  KEY `fiscal_document_issued_from_fk_idx` (`issuer_address`),
  KEY `fiscal_document_store_fk_idx` (`store`),
  KEY `fiscal_document_issued_at_fk_idx` (`issued_at`),
  KEY `fiscal_document_issuer_regime_idx` (`issuer_regime`),
  KEY `fiscal_document_usage_idx` (`usage`),
  CONSTRAINT `fiscal_document_creator_fk` FOREIGN KEY (`creator`) REFERENCES `employee` (`employee_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `fiscal_document_customer_fk` FOREIGN KEY (`customer`) REFERENCES `customer` (`customer_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `fiscal_document_issued_at_fk` FOREIGN KEY (`issued_at`) REFERENCES `address` (`address_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `fiscal_document_issuer_address_fk` FOREIGN KEY (`issuer_address`) REFERENCES `address` (`address_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `fiscal_document_issuer_fk` FOREIGN KEY (`issuer`) REFERENCES `taxpayer_issuer` (`taxpayer_issuer_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `fiscal_document_issuer_regime_fk` FOREIGN KEY (`issuer_regime`) REFERENCES `sat_tax_regime` (`sat_tax_regime_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `fiscal_document_recipient_address_fk` FOREIGN KEY (`recipient_address`) REFERENCES `address` (`address_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `fiscal_document_store_fk` FOREIGN KEY (`store`) REFERENCES `store` (`store_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `fiscal_document_updater_fk` FOREIGN KEY (`updater`) REFERENCES `employee` (`employee_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `fiscal_document_usage_fk` FOREIGN KEY (`usage`) REFERENCES `sat_cfdi_usage` (`sat_cfdi_usage_id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `fiscal_document_detail`
--

DROP TABLE IF EXISTS `fiscal_document_detail`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `fiscal_document_detail` (
  `fiscal_document_detail_id` int(11) NOT NULL AUTO_INCREMENT,
  `document` int(11) NOT NULL,
  `product` int(11) NOT NULL,
  `order_detail` int(11) DEFAULT NULL,
  `product_service` varchar(8) COLLATE utf8_unicode_ci DEFAULT NULL,
  `product_code` varchar(35) COLLATE utf8_unicode_ci DEFAULT NULL,
  `product_name` varchar(1000) COLLATE utf8_unicode_ci NOT NULL,
  `unit_of_measurement` varchar(3) COLLATE utf8_unicode_ci DEFAULT NULL,
  `unit_of_measurement_name` varchar(25) COLLATE utf8_unicode_ci NOT NULL,
  `quantity` decimal(18,4) NOT NULL,
  `price` decimal(18,7) NOT NULL,
  `discount` decimal(9,8) NOT NULL,
  `tax_rate` decimal(7,6) NOT NULL,
  `exchange_rate` decimal(8,4) NOT NULL DEFAULT 1.0000,
  `currency` int(11) NOT NULL,
  `tax_included` tinyint(1) NOT NULL,
  `comment` varchar(1000) COLLATE utf8_unicode_ci DEFAULT NULL,
  PRIMARY KEY (`fiscal_document_detail_id`),
  KEY `fiscal_document_detail_document_fk_idx` (`document`),
  KEY `fiscal_document_detail_product_fk_idx` (`product`),
  KEY `fiscal_document_detail_order_detail_fk_idx` (`order_detail`),
  KEY `fiscal_document_product_service_idx` (`product_service`),
  KEY `fiscal_document_uom_idx` (`unit_of_measurement`),
  CONSTRAINT `fiscal_document_detail_document_fk` FOREIGN KEY (`document`) REFERENCES `fiscal_document` (`fiscal_document_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `fiscal_document_detail_order_detail_fk` FOREIGN KEY (`order_detail`) REFERENCES `sales_order_detail` (`sales_order_detail_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `fiscal_document_detail_product_fk` FOREIGN KEY (`product`) REFERENCES `product` (`product_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `fiscal_document_product_service_fk` FOREIGN KEY (`product_service`) REFERENCES `sat_product_service` (`sat_product_service_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `fiscal_document_uom_fk` FOREIGN KEY (`unit_of_measurement`) REFERENCES `sat_unit_of_measurement` (`sat_unit_of_measurement_id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `fiscal_document_relation`
--

DROP TABLE IF EXISTS `fiscal_document_relation`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `fiscal_document_relation` (
  `fiscal_document_relation_id` int(11) NOT NULL AUTO_INCREMENT,
  `document` int(11) NOT NULL,
  `relation` int(11) NOT NULL,
  `exchange_rate` decimal(8,4) NOT NULL DEFAULT 1.0000,
  `installment` int(11) NOT NULL,
  `previous_balance` decimal(18,2) NOT NULL,
  `amount` decimal(18,2) NOT NULL,
  PRIMARY KEY (`fiscal_document_relation_id`),
  UNIQUE KEY `document_relation_idx` (`document`,`relation`),
  KEY `fiscal_document_relation_document_idx` (`document`),
  KEY `fiscal_document_relation_relation_fk` (`relation`),
  CONSTRAINT `fiscal_document_relation_document_fk` FOREIGN KEY (`document`) REFERENCES `fiscal_document` (`fiscal_document_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `fiscal_document_relation_relation_fk` FOREIGN KEY (`relation`) REFERENCES `fiscal_document` (`fiscal_document_id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `fiscal_document_xml`
--

DROP TABLE IF EXISTS `fiscal_document_xml`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `fiscal_document_xml` (
  `fiscal_document_xml_id` int(11) NOT NULL,
  `data` mediumtext COLLATE utf8_unicode_ci NOT NULL,
  PRIMARY KEY (`fiscal_document_xml_id`),
  CONSTRAINT `fiscal_document_xml_id_fk` FOREIGN KEY (`fiscal_document_xml_id`) REFERENCES `fiscal_document` (`fiscal_document_id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `inventory_issue`
--

DROP TABLE IF EXISTS `inventory_issue`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `inventory_issue` (
  `inventory_issue_id` int(11) NOT NULL AUTO_INCREMENT,
  `store` int(11) NOT NULL,
  `serial` int(11) DEFAULT NULL,
  `creation_time` datetime NOT NULL,
  `modification_time` datetime NOT NULL,
  `creator` int(11) NOT NULL,
  `updater` int(11) NOT NULL,
  `warehouse` int(11) NOT NULL,
  `completed` tinyint(1) NOT NULL DEFAULT 0,
  `cancelled` tinyint(1) NOT NULL DEFAULT 0,
  `comment` varchar(500) COLLATE utf8_unicode_ci DEFAULT NULL,
  `supplier_return` int(11) DEFAULT NULL,
  PRIMARY KEY (`inventory_issue_id`),
  UNIQUE KEY `inventory_issue_store_serial_idx` (`store`,`serial`),
  KEY `inventory_issue_employee_creator_idx` (`creator`),
  KEY `inventory_issue_employee_updater_idx` (`updater`),
  KEY `inventory_issue_warehouse_idx` (`warehouse`),
  KEY `inventory_issue_supplier_return_idx` (`supplier_return`),
  KEY `inventory_issue_store_idx` (`store`),
  CONSTRAINT `inventory_issue_employee_creator_fk` FOREIGN KEY (`creator`) REFERENCES `employee` (`employee_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `inventory_issue_employee_updater_fk` FOREIGN KEY (`updater`) REFERENCES `employee` (`employee_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `inventory_issue_store_fk` FOREIGN KEY (`store`) REFERENCES `store` (`store_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `inventory_issue_supplier_return_fk` FOREIGN KEY (`supplier_return`) REFERENCES `supplier_return` (`supplier_return_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `inventory_issue_warehouse_fk` FOREIGN KEY (`warehouse`) REFERENCES `warehouse` (`warehouse_id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `inventory_issue_detail`
--

DROP TABLE IF EXISTS `inventory_issue_detail`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `inventory_issue_detail` (
  `issue_detail_id` int(11) NOT NULL AUTO_INCREMENT,
  `issue` int(11) NOT NULL,
  `product` int(11) NOT NULL,
  `quantity` decimal(18,4) NOT NULL,
  `product_code` varchar(25) COLLATE utf8_unicode_ci DEFAULT NULL,
  `product_name` varchar(250) COLLATE utf8_unicode_ci DEFAULT NULL,
  PRIMARY KEY (`issue_detail_id`),
  KEY `inventory_issue_detail_inventory_issue_idx` (`issue`),
  KEY `inventory_issue_detail_product_idx` (`product`),
  CONSTRAINT `inventory_issue_detail_inventory_issue` FOREIGN KEY (`issue`) REFERENCES `inventory_issue` (`inventory_issue_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `inventory_issue_detail_product` FOREIGN KEY (`product`) REFERENCES `product` (`product_id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `inventory_receipt`
--

DROP TABLE IF EXISTS `inventory_receipt`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `inventory_receipt` (
  `inventory_receipt_id` int(11) NOT NULL AUTO_INCREMENT,
  `store` int(11) NOT NULL,
  `serial` int(11) DEFAULT NULL,
  `creation_time` datetime NOT NULL,
  `modification_time` datetime NOT NULL,
  `creator` int(11) NOT NULL,
  `updater` int(11) NOT NULL,
  `warehouse` int(11) NOT NULL,
  `purchase_order` int(11) DEFAULT NULL,
  `completed` tinyint(1) NOT NULL DEFAULT 0,
  `cancelled` tinyint(1) NOT NULL DEFAULT 0,
  `comment` varchar(500) COLLATE utf8_unicode_ci DEFAULT NULL,
  PRIMARY KEY (`inventory_receipt_id`),
  UNIQUE KEY `inventory_receipt_store_serial_idx` (`store`,`serial`),
  KEY `inventory_receipt_warehouse_idx` (`warehouse`),
  KEY `inventory_receipt_purchase_order_idx` (`purchase_order`),
  KEY `inventory_receipt_creator_idx` (`creator`),
  KEY `inventory_receipt_updater_idx` (`updater`),
  KEY `inventory_receipt_store_idx` (`store`),
  CONSTRAINT `inventory_receipt_creator_fk` FOREIGN KEY (`creator`) REFERENCES `employee` (`employee_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `inventory_receipt_purchase_order_fk` FOREIGN KEY (`purchase_order`) REFERENCES `purchase_order` (`purchase_order_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `inventory_receipt_store_fk` FOREIGN KEY (`store`) REFERENCES `store` (`store_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `inventory_receipt_updater_fk` FOREIGN KEY (`updater`) REFERENCES `employee` (`employee_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `inventory_receipt_warehouse_fk` FOREIGN KEY (`warehouse`) REFERENCES `warehouse` (`warehouse_id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `inventory_receipt_detail`
--

DROP TABLE IF EXISTS `inventory_receipt_detail`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `inventory_receipt_detail` (
  `receipt_detail_id` int(11) NOT NULL AUTO_INCREMENT,
  `receipt` int(11) NOT NULL,
  `product` int(11) NOT NULL,
  `quantity` decimal(18,4) NOT NULL,
  `product_code` varchar(25) COLLATE utf8_unicode_ci DEFAULT NULL,
  `product_name` varchar(250) COLLATE utf8_unicode_ci DEFAULT NULL,
  `quantity_ordered` decimal(18,4) NOT NULL,
  PRIMARY KEY (`receipt_detail_id`),
  KEY `inventory_receipt_detail_inventory_receipt_idx` (`receipt`),
  KEY `inventory_receipt_detail_product_idx` (`product`),
  CONSTRAINT `inventory_receipt_detail_inventory_receipt` FOREIGN KEY (`receipt`) REFERENCES `inventory_receipt` (`inventory_receipt_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `inventory_receipt_detail_product` FOREIGN KEY (`product`) REFERENCES `product` (`product_id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `inventory_transfer`
--

DROP TABLE IF EXISTS `inventory_transfer`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `inventory_transfer` (
  `inventory_transfer_id` int(11) NOT NULL AUTO_INCREMENT,
  `store` int(11) NOT NULL,
  `serial` int(11) DEFAULT NULL,
  `creation_time` datetime NOT NULL,
  `modification_time` datetime NOT NULL,
  `creator` int(11) NOT NULL,
  `updater` int(11) NOT NULL,
  `warehouse` int(11) NOT NULL,
  `warehouse_to` int(11) NOT NULL,
  `completed` tinyint(1) NOT NULL DEFAULT 0,
  `cancelled` tinyint(1) NOT NULL DEFAULT 0,
  `comment` varchar(500) COLLATE utf8_unicode_ci DEFAULT NULL,
  PRIMARY KEY (`inventory_transfer_id`),
  UNIQUE KEY `inventory_transfer_store_serial_idx` (`store`,`serial`),
  KEY `inventory_transfer_from_idx` (`warehouse`),
  KEY `inventory_transfer_to_idx` (`warehouse_to`),
  KEY `inventory_transfer_creator_idx` (`creator`),
  KEY `inventory_transfer_updater_idx` (`updater`),
  KEY `inventory_transfer_store_idx` (`store`),
  CONSTRAINT `inventory_transfer_creator_fk` FOREIGN KEY (`creator`) REFERENCES `employee` (`employee_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `inventory_transfer_from_fk` FOREIGN KEY (`warehouse`) REFERENCES `warehouse` (`warehouse_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `inventory_transfer_store_fk` FOREIGN KEY (`store`) REFERENCES `store` (`store_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `inventory_transfer_to_fk` FOREIGN KEY (`warehouse_to`) REFERENCES `warehouse` (`warehouse_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `inventory_transfer_updater_fk` FOREIGN KEY (`updater`) REFERENCES `employee` (`employee_id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `inventory_transfer_detail`
--

DROP TABLE IF EXISTS `inventory_transfer_detail`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `inventory_transfer_detail` (
  `transfer_detail_id` int(11) NOT NULL AUTO_INCREMENT,
  `transfer` int(11) NOT NULL,
  `product` int(11) NOT NULL,
  `quantity` decimal(18,4) NOT NULL,
  `product_code` varchar(25) COLLATE utf8_unicode_ci DEFAULT NULL,
  `product_name` varchar(250) COLLATE utf8_unicode_ci DEFAULT NULL,
  PRIMARY KEY (`transfer_detail_id`),
  KEY `inventory_transfer_detail_inventory_transfer_idx` (`transfer`),
  KEY `inventory_transfer_detail_product_idx` (`product`),
  CONSTRAINT `inventory_transfer_detail_inventory_transfer` FOREIGN KEY (`transfer`) REFERENCES `inventory_transfer` (`inventory_transfer_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `inventory_transfer_detail_product` FOREIGN KEY (`product`) REFERENCES `product` (`product_id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `label`
--

DROP TABLE IF EXISTS `label`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `label` (
  `label_id` int(11) NOT NULL AUTO_INCREMENT,
  `name` varchar(250) COLLATE utf8_unicode_ci NOT NULL,
  `comment` varchar(500) COLLATE utf8_unicode_ci DEFAULT NULL,
  PRIMARY KEY (`label_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `lot_serial_rqmt`
--

DROP TABLE IF EXISTS `lot_serial_rqmt`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `lot_serial_rqmt` (
  `lot_serial_rqmt_id` int(11) NOT NULL AUTO_INCREMENT,
  `source` int(11) NOT NULL,
  `reference` int(11) NOT NULL,
  `warehouse` int(11) NOT NULL,
  `product` int(11) NOT NULL,
  `quantity` decimal(18,4) NOT NULL,
  PRIMARY KEY (`lot_serial_rqmt_id`),
  KEY `lot_serial_rqmt_warehouse_fk_idx` (`warehouse`),
  KEY `lot_serial_rqmt_product_fk_idx` (`product`),
  CONSTRAINT `lot_serial_rqmt_product_fk` FOREIGN KEY (`product`) REFERENCES `product` (`product_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `lot_serial_rqmt_warehouse_fk` FOREIGN KEY (`warehouse`) REFERENCES `warehouse` (`warehouse_id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `lot_serial_tracking`
--

DROP TABLE IF EXISTS `lot_serial_tracking`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `lot_serial_tracking` (
  `lot_serial_tracking_id` int(11) NOT NULL AUTO_INCREMENT,
  `source` int(11) NOT NULL,
  `reference` int(11) NOT NULL,
  `date` datetime NOT NULL,
  `warehouse` int(11) NOT NULL,
  `product` int(11) NOT NULL,
  `quantity` decimal(18,4) NOT NULL,
  `lot_number` varchar(50) COLLATE utf8_unicode_ci DEFAULT NULL,
  `expiration_date` date DEFAULT NULL,
  `serial_number` varchar(50) COLLATE utf8_unicode_ci DEFAULT NULL,
  PRIMARY KEY (`lot_serial_tracking_id`),
  KEY `lot_serial_tracking_warehouse_idx` (`warehouse`),
  KEY `lot_serial_tracking_product_idx` (`product`),
  KEY `lot_serial_tracking_w_p_idx` (`warehouse`,`product`),
  KEY `lot_serial_tracking_w_p_l_idx` (`warehouse`,`product`,`lot_number`,`expiration_date`),
  KEY `lot_serial_tracking_w_p_l_s_idx` (`warehouse`,`product`,`lot_number`,`expiration_date`,`serial_number`),
  CONSTRAINT `lot_serial_tracking_product_fk` FOREIGN KEY (`product`) REFERENCES `product` (`product_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `lot_serial_tracking_warehouse_fk` FOREIGN KEY (`warehouse`) REFERENCES `warehouse` (`warehouse_id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `notarization`
--

DROP TABLE IF EXISTS `notarization`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `notarization` (
  `notarization_id` int(11) NOT NULL AUTO_INCREMENT,
  `requester` int(11) NOT NULL,
  `notary_office` varchar(256) COLLATE utf8_unicode_ci NOT NULL,
  `date` datetime NOT NULL,
  `document_description` varchar(512) COLLATE utf8_unicode_ci NOT NULL,
  `amount` decimal(18,4) NOT NULL,
  `payment_date` datetime NOT NULL,
  `delivery_date` datetime NOT NULL,
  `comment` varchar(1024) COLLATE utf8_unicode_ci DEFAULT NULL,
  PRIMARY KEY (`notarization_id`),
  KEY `notarization_requester_idx` (`requester`),
  CONSTRAINT `notarization_requester_fk` FOREIGN KEY (`requester`) REFERENCES `employee` (`employee_id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `payment_method_option`
--

DROP TABLE IF EXISTS `payment_method_option`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `payment_method_option` (
  `payment_method_option_id` int(11) NOT NULL AUTO_INCREMENT,
  `warehouse` int(11) DEFAULT NULL,
  `store` int(11) NOT NULL,
  `name` varchar(50) COLLATE utf8_unicode_ci NOT NULL,
  `number_of_payments` tinyint(4) NOT NULL DEFAULT 1,
  `display_on_ticket` tinyint(1) NOT NULL,
  `payment_method` int(11) NOT NULL,
  `commission` decimal(10,3) NOT NULL,
  `enabled` tinyint(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`payment_method_option_id`),
  KEY `FK_payment_method_charge_store` (`store`),
  CONSTRAINT `FK_payment_method_charge_store` FOREIGN KEY (`store`) REFERENCES `store` (`store_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `payment_on_delivery`
--

DROP TABLE IF EXISTS `payment_on_delivery`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `payment_on_delivery` (
  `payment_on_delivery_id` int(11) NOT NULL AUTO_INCREMENT,
  `customer_payment` int(11) NOT NULL,
  `cash_session` int(11) DEFAULT NULL,
  `paid` tinyint(1) NOT NULL DEFAULT 0,
  `date` datetime NOT NULL DEFAULT current_timestamp(),
  PRIMARY KEY (`payment_on_delivery_id`),
  KEY `payment_on_delivery_customer_payment` (`customer_payment`),
  KEY `payment_on_delivery_cash_session` (`cash_session`),
  CONSTRAINT `payments_on_deliveries_cash_session_fk` FOREIGN KEY (`cash_session`) REFERENCES `cash_session` (`cash_session_id`),
  CONSTRAINT `payments_on_deliveries_customer_payment_fk` FOREIGN KEY (`customer_payment`) REFERENCES `customer_payment` (`customer_payment_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `point_sale`
--

DROP TABLE IF EXISTS `point_sale`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `point_sale` (
  `point_sale_id` int(11) NOT NULL AUTO_INCREMENT,
  `store` int(11) NOT NULL DEFAULT 1,
  `code` varchar(25) COLLATE utf8_unicode_ci NOT NULL,
  `name` varchar(250) COLLATE utf8_unicode_ci NOT NULL,
  `warehouse` int(11) NOT NULL,
  `comment` varchar(500) COLLATE utf8_unicode_ci DEFAULT NULL,
  PRIMARY KEY (`point_sale_id`),
  UNIQUE KEY `code_UNIQUE` (`code`),
  KEY `point_sale_store_fk_idx` (`store`),
  KEY `point_sale_warehouse_fk_idx` (`warehouse`),
  CONSTRAINT `point_sale_store_fk` FOREIGN KEY (`store`) REFERENCES `store` (`store_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `point_sale_warehouse_fk` FOREIGN KEY (`warehouse`) REFERENCES `warehouse` (`warehouse_id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `postal_code`
--

DROP TABLE IF EXISTS `postal_code`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `postal_code` (
  `postal_code_id` int(11) NOT NULL AUTO_INCREMENT,
  `code` int(5) unsigned zerofill NOT NULL,
  `neighborhood` varchar(150) COLLATE utf8_unicode_ci NOT NULL,
  `borough` varchar(50) COLLATE utf8_unicode_ci NOT NULL,
  `state` varchar(50) COLLATE utf8_unicode_ci NOT NULL,
  `city` varchar(50) COLLATE utf8_unicode_ci DEFAULT NULL,
  `country` varchar(50) COLLATE utf8_unicode_ci NOT NULL,
  PRIMARY KEY (`postal_code_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `price_list`
--

DROP TABLE IF EXISTS `price_list`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `price_list` (
  `price_list_id` int(11) NOT NULL AUTO_INCREMENT,
  `name` varchar(250) COLLATE utf8_unicode_ci NOT NULL,
  `high_profit_margin` decimal(5,4) NOT NULL,
  `low_profit_margin` decimal(5,4) NOT NULL,
  PRIMARY KEY (`price_list_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `product`
--

DROP TABLE IF EXISTS `product`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `product` (
  `product_id` int(11) NOT NULL AUTO_INCREMENT,
  `code` varchar(25) COLLATE utf8_unicode_ci NOT NULL,
  `name` varchar(250) COLLATE utf8_unicode_ci NOT NULL,
  `photo` varchar(255) COLLATE utf8_unicode_ci DEFAULT NULL,
  `sku` varchar(50) COLLATE utf8_unicode_ci DEFAULT NULL,
  `brand` varchar(100) COLLATE utf8_unicode_ci DEFAULT NULL,
  `model` varchar(100) COLLATE utf8_unicode_ci DEFAULT NULL,
  `location` varchar(50) COLLATE utf8_unicode_ci DEFAULT NULL,
  `unit_of_measurement` varchar(3) COLLATE utf8_unicode_ci NOT NULL,
  `stockable` tinyint(1) NOT NULL,
  `perishable` tinyint(1) NOT NULL,
  `seriable` tinyint(1) NOT NULL,
  `purchasable` tinyint(1) NOT NULL,
  `salable` tinyint(1) NOT NULL,
  `invoiceable` tinyint(1) NOT NULL,
  `tax_rate` decimal(7,6) NOT NULL,
  `tax_included` tinyint(1) NOT NULL,
  `price_type` tinyint(4) NOT NULL,
  `currency` int(11) NOT NULL,
  `min_order_qty` int(11) NOT NULL,
  `comment` varchar(500) COLLATE utf8_unicode_ci DEFAULT NULL,
  `supplier` int(11) DEFAULT NULL,
  `key` varchar(8) COLLATE utf8_unicode_ci DEFAULT NULL,
  PRIMARY KEY (`product_id`),
  UNIQUE KEY `code_UNIQUE` (`code`),
  KEY `product_supplier_idx` (`supplier`),
  KEY `product_brand_idx` (`brand`),
  KEY `product_uom_idx` (`unit_of_measurement`),
  KEY `product_key_idx` (`key`),
  CONSTRAINT `product_key_fk` FOREIGN KEY (`key`) REFERENCES `sat_product_service` (`sat_product_service_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `product_supplier_fk` FOREIGN KEY (`supplier`) REFERENCES `supplier` (`supplier_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `product_uom_fk` FOREIGN KEY (`unit_of_measurement`) REFERENCES `sat_unit_of_measurement` (`sat_unit_of_measurement_id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `product_label`
--

DROP TABLE IF EXISTS `product_label`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `product_label` (
  `product` int(11) NOT NULL,
  `label` int(11) NOT NULL,
  PRIMARY KEY (`product`,`label`),
  KEY `product_label_product_idx` (`product`),
  KEY `product_label_label_idx` (`label`),
  CONSTRAINT `product_label_label_fk` FOREIGN KEY (`label`) REFERENCES `label` (`label_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `product_label_product_fk` FOREIGN KEY (`product`) REFERENCES `product` (`product_id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `product_price`
--

DROP TABLE IF EXISTS `product_price`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `product_price` (
  `product_price_id` int(11) NOT NULL AUTO_INCREMENT,
  `product` int(11) NOT NULL,
  `list` int(11) NOT NULL,
  `price` decimal(18,4) NOT NULL,
  PRIMARY KEY (`product_price_id`),
  UNIQUE KEY `product_price_product_list_idx` (`product`,`list`),
  KEY `product_price_product_idx` (`product`),
  KEY `product_price_list_idx` (`list`),
  CONSTRAINT `product_price_list_fk` FOREIGN KEY (`list`) REFERENCES `price_list` (`price_list_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `product_price_product_fk` FOREIGN KEY (`product`) REFERENCES `product` (`product_id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `purchase_order`
--

DROP TABLE IF EXISTS `purchase_order`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `purchase_order` (
  `purchase_order_id` int(11) NOT NULL AUTO_INCREMENT,
  `supplier` int(11) NOT NULL,
  `creation_time` datetime NOT NULL,
  `modification_time` datetime NOT NULL,
  `creator` int(11) NOT NULL,
  `updater` int(11) NOT NULL,
  `completed` tinyint(1) NOT NULL DEFAULT 0,
  `cancelled` tinyint(1) NOT NULL DEFAULT 0,
  `invoice_number` varchar(50) COLLATE utf8_unicode_ci DEFAULT NULL,
  `comment` varchar(500) COLLATE utf8_unicode_ci DEFAULT NULL,
  PRIMARY KEY (`purchase_order_id`),
  KEY `purchase_order_supplier_fk_idx` (`supplier`),
  KEY `purchase_order_employee_creator_fk_idx` (`creator`),
  KEY `purchase_order_employee_updater_fk_idx` (`updater`),
  CONSTRAINT `purchase_order_employee_creator_fk` FOREIGN KEY (`creator`) REFERENCES `employee` (`employee_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `purchase_order_employee_updater_fk` FOREIGN KEY (`updater`) REFERENCES `employee` (`employee_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `purchase_order_supplier_fk` FOREIGN KEY (`supplier`) REFERENCES `supplier` (`supplier_id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `purchase_order_detail`
--

DROP TABLE IF EXISTS `purchase_order_detail`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `purchase_order_detail` (
  `purchase_order_detail_id` int(11) NOT NULL AUTO_INCREMENT,
  `purchase_order` int(11) NOT NULL,
  `product` int(11) NOT NULL,
  `warehouse` int(11) DEFAULT NULL,
  `quantity` decimal(18,4) NOT NULL,
  `price` decimal(18,7) NOT NULL,
  `discount` decimal(9,8) NOT NULL,
  `tax_rate` decimal(5,4) NOT NULL,
  `product_code` varchar(25) COLLATE utf8_unicode_ci NOT NULL,
  `product_name` varchar(250) COLLATE utf8_unicode_ci NOT NULL,
  `exchange_rate` decimal(8,4) NOT NULL DEFAULT 1.0000,
  `currency` int(11) NOT NULL,
  `tax_included` tinyint(1) NOT NULL,
  PRIMARY KEY (`purchase_order_detail_id`),
  KEY `p_order_detail_p_order_fk_idx` (`purchase_order`),
  KEY `p_order_detail_product_fk_idx` (`product`),
  KEY `p_order_detail_warehouse_fk_idx` (`warehouse`),
  CONSTRAINT `p_order_detail_p_order_fk` FOREIGN KEY (`purchase_order`) REFERENCES `purchase_order` (`purchase_order_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `p_order_detail_product_fk` FOREIGN KEY (`product`) REFERENCES `product` (`product_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `p_order_detail_warehouse_fk` FOREIGN KEY (`warehouse`) REFERENCES `warehouse` (`warehouse_id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `sales_order`
--

DROP TABLE IF EXISTS `sales_order`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `sales_order` (
  `sales_order_id` int(11) NOT NULL AUTO_INCREMENT,
  `store` int(11) NOT NULL,
  `serial` int(11) NOT NULL,
  `point_sale` int(11) NOT NULL,
  `salesperson` int(11) NOT NULL,
  `customer` int(11) NOT NULL,
  `payment_terms` tinyint(4) NOT NULL,
  `date` datetime NOT NULL,
  `promise_date` datetime NOT NULL,
  `recipient` varchar(13) COLLATE utf8_unicode_ci DEFAULT NULL,
  `recipient_name` varchar(250) COLLATE utf8_unicode_ci DEFAULT NULL,
  `recipient_address` int(11) DEFAULT NULL,
  `due_date` datetime NOT NULL,
  `completed` tinyint(1) NOT NULL DEFAULT 0,
  `cancelled` tinyint(1) NOT NULL DEFAULT 0,
  `paid` tinyint(1) NOT NULL DEFAULT 0,
  `creator` int(11) NOT NULL,
  `updater` int(11) NOT NULL,
  `creation_time` datetime NOT NULL,
  `modification_time` datetime NOT NULL,
  `contact` int(11) DEFAULT NULL,
  `ship_to` int(11) DEFAULT NULL,
  `delivered` tinyint(1) NOT NULL,
  `comment` varchar(500) COLLATE utf8_unicode_ci DEFAULT NULL,
  `currency` int(11) NOT NULL,
  `exchange_rate` decimal(8,4) NOT NULL DEFAULT 1.0000,
  `customer_name` varchar(100) COLLATE utf8_unicode_ci DEFAULT NULL,
  `customer_shipto` varchar(200) COLLATE utf8_unicode_ci DEFAULT NULL,
  PRIMARY KEY (`sales_order_id`),
  UNIQUE KEY `sales_order_store_serial_idx` (`store`,`serial`),
  KEY `sales_order_customer_fk_idx` (`customer`),
  KEY `sales_order_point_sale_fk_idx` (`point_sale`),
  KEY `sales_order_employed_fk_idx` (`salesperson`),
  KEY `sales_order_store_fk_idx` (`store`),
  KEY `sales_order_creator_fk_idx` (`creator`),
  KEY `sales_order_updater_fk_idx` (`updater`),
  KEY `sales_order_ship_to_fk_idx` (`ship_to`),
  KEY `sales_order_contact_fk_idx` (`contact`),
  KEY `FK_sales_order_address` (`recipient_address`),
  CONSTRAINT `FK_sales_order_address` FOREIGN KEY (`recipient_address`) REFERENCES `address` (`address_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `sales_order_contact_fk` FOREIGN KEY (`contact`) REFERENCES `contact` (`contact_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `sales_order_creator_fk` FOREIGN KEY (`creator`) REFERENCES `employee` (`employee_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `sales_order_customer_fk` FOREIGN KEY (`customer`) REFERENCES `customer` (`customer_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `sales_order_employed_fk` FOREIGN KEY (`salesperson`) REFERENCES `employee` (`employee_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `sales_order_point_sale_fk` FOREIGN KEY (`point_sale`) REFERENCES `point_sale` (`point_sale_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `sales_order_ship_to_fk` FOREIGN KEY (`ship_to`) REFERENCES `address` (`address_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `sales_order_store_fk` FOREIGN KEY (`store`) REFERENCES `store` (`store_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `sales_order_updater_fk` FOREIGN KEY (`updater`) REFERENCES `employee` (`employee_id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `sales_order_detail`
--

DROP TABLE IF EXISTS `sales_order_detail`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `sales_order_detail` (
  `sales_order_detail_id` int(11) NOT NULL AUTO_INCREMENT,
  `sales_order` int(11) NOT NULL,
  `product` int(11) NOT NULL,
  `quantity` decimal(18,4) NOT NULL,
  `cost` decimal(18,4) NOT NULL,
  `price` decimal(18,7) NOT NULL,
  `discount` decimal(9,8) NOT NULL,
  `tax_rate` decimal(5,4) NOT NULL,
  `product_code` varchar(25) COLLATE utf8_unicode_ci NOT NULL,
  `product_name` varchar(250) COLLATE utf8_unicode_ci NOT NULL,
  `delivery` tinyint(1) NOT NULL,
  `warehouse` int(11) DEFAULT NULL,
  `exchange_rate` decimal(8,4) NOT NULL DEFAULT 1.0000,
  `currency` int(11) NOT NULL,
  `tax_included` tinyint(1) NOT NULL,
  `comment` varchar(500) COLLATE utf8_unicode_ci DEFAULT NULL,
  PRIMARY KEY (`sales_order_detail_id`),
  KEY `sales_order_detail_sales_order_fk_idx` (`sales_order`),
  KEY `sales_order_detail_product_fk_idx` (`product`),
  KEY `sales_order_detail_warehouse_fk_idx` (`warehouse`),
  CONSTRAINT `sales_order_detail_product_fk` FOREIGN KEY (`product`) REFERENCES `product` (`product_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `sales_order_detail_sales_order_fk` FOREIGN KEY (`sales_order`) REFERENCES `sales_order` (`sales_order_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `sales_order_detail_warehouse_fk` FOREIGN KEY (`warehouse`) REFERENCES `warehouse` (`warehouse_id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `sales_order_payment`
--

DROP TABLE IF EXISTS `sales_order_payment`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `sales_order_payment` (
  `sales_order_payment_id` int(11) NOT NULL AUTO_INCREMENT,
  `sales_order` int(11) NOT NULL,
  `customer_payment` int(11) NOT NULL,
  `amount` decimal(18,4) NOT NULL,
  `amount_change` decimal(18,4) NOT NULL,
  PRIMARY KEY (`sales_order_payment_id`),
  UNIQUE KEY `sales_order_payment_sales_order_customer_payment_idx` (`sales_order`,`customer_payment`),
  KEY `sales_order_payment_sales_order_idx` (`sales_order`),
  KEY `sales_order_payment_customer_payment_idx` (`customer_payment`),
  CONSTRAINT `sales_order_payment_customer_payment_fk` FOREIGN KEY (`customer_payment`) REFERENCES `customer_payment` (`customer_payment_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `sales_order_payment_sales_order_fk` FOREIGN KEY (`sales_order`) REFERENCES `sales_order` (`sales_order_id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `sales_quote`
--

DROP TABLE IF EXISTS `sales_quote`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `sales_quote` (
  `sales_quote_id` int(11) NOT NULL AUTO_INCREMENT,
  `store` int(11) NOT NULL,
  `serial` int(11) NOT NULL,
  `date` datetime NOT NULL,
  `salesperson` int(11) NOT NULL,
  `customer` int(11) NOT NULL,
  `payment_terms` tinyint(4) NOT NULL,
  `due_date` datetime NOT NULL,
  `completed` tinyint(1) NOT NULL DEFAULT 0,
  `cancelled` tinyint(1) NOT NULL DEFAULT 0,
  `creator` int(11) NOT NULL,
  `updater` int(11) NOT NULL,
  `creation_time` datetime NOT NULL,
  `modification_time` datetime NOT NULL,
  `contact` int(11) DEFAULT NULL,
  `ship_to` int(11) DEFAULT NULL,
  `comment` varchar(1024) COLLATE utf8_unicode_ci DEFAULT NULL,
  `currency` int(11) NOT NULL,
  `exchange_rate` decimal(8,4) NOT NULL DEFAULT 1.0000,
  PRIMARY KEY (`sales_quote_id`),
  KEY `sales_quote_customer_fk_idx` (`customer`),
  KEY `sales_quote_employed_fk_idx` (`salesperson`),
  KEY `sales_quote_store_fk_idx` (`store`),
  KEY `sales_quote_creator_idx` (`creator`),
  KEY `sales_quote_updater_idx` (`updater`),
  KEY `sales_quote_ship_to_idx` (`ship_to`),
  KEY `sales_quote_contact_idx` (`contact`),
  CONSTRAINT `sales_quote_contact_fk` FOREIGN KEY (`contact`) REFERENCES `contact` (`contact_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `sales_quote_creator_fk` FOREIGN KEY (`creator`) REFERENCES `employee` (`employee_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `sales_quote_customer_fk` FOREIGN KEY (`customer`) REFERENCES `customer` (`customer_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `sales_quote_employed_fk` FOREIGN KEY (`salesperson`) REFERENCES `employee` (`employee_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `sales_quote_ship_to_fk` FOREIGN KEY (`ship_to`) REFERENCES `address` (`address_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `sales_quote_store_fk` FOREIGN KEY (`store`) REFERENCES `store` (`store_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `sales_quote_updater_fk` FOREIGN KEY (`updater`) REFERENCES `employee` (`employee_id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `sales_quote_detail`
--

DROP TABLE IF EXISTS `sales_quote_detail`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `sales_quote_detail` (
  `sales_quote_detail_id` int(11) NOT NULL AUTO_INCREMENT,
  `sales_quote` int(11) NOT NULL,
  `product` int(11) NOT NULL,
  `quantity` decimal(18,4) NOT NULL,
  `price` decimal(18,7) NOT NULL,
  `discount` decimal(9,8) NOT NULL,
  `tax_rate` decimal(5,4) NOT NULL,
  `product_code` varchar(25) COLLATE utf8_unicode_ci NOT NULL,
  `product_name` varchar(250) COLLATE utf8_unicode_ci NOT NULL,
  `exchange_rate` decimal(8,4) NOT NULL DEFAULT 1.0000,
  `currency` int(11) NOT NULL,
  `tax_included` tinyint(1) NOT NULL,
  `comment` varchar(1024) COLLATE utf8_unicode_ci DEFAULT NULL,
  PRIMARY KEY (`sales_quote_detail_id`),
  KEY `sales_quote_detail_product_idx` (`product`),
  KEY `sales_quote_detail_quote_idx` (`sales_quote`),
  CONSTRAINT `sales_quote_detail_product_fk` FOREIGN KEY (`product`) REFERENCES `product` (`product_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `sales_quote_detail_quote_fk` FOREIGN KEY (`sales_quote`) REFERENCES `sales_quote` (`sales_quote_id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `sat_cfdi_usage`
--

DROP TABLE IF EXISTS `sat_cfdi_usage`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `sat_cfdi_usage` (
  `sat_cfdi_usage_id` varchar(3) COLLATE utf8_unicode_ci NOT NULL,
  `description` varchar(256) COLLATE utf8_unicode_ci NOT NULL,
  PRIMARY KEY (`sat_cfdi_usage_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `sat_country`
--

DROP TABLE IF EXISTS `sat_country`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `sat_country` (
  `sat_country_id` varchar(3) COLLATE utf8_unicode_ci NOT NULL,
  `description` varchar(256) COLLATE utf8_unicode_ci NOT NULL,
  PRIMARY KEY (`sat_country_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `sat_currency`
--

DROP TABLE IF EXISTS `sat_currency`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `sat_currency` (
  `sat_currency_id` varchar(3) COLLATE utf8_unicode_ci NOT NULL,
  `description` varchar(256) COLLATE utf8_unicode_ci NOT NULL,
  PRIMARY KEY (`sat_currency_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `sat_postal_code`
--

DROP TABLE IF EXISTS `sat_postal_code`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `sat_postal_code` (
  `sat_postal_code_id` varchar(5) COLLATE utf8_unicode_ci NOT NULL,
  `state` varchar(4) COLLATE utf8_unicode_ci NOT NULL,
  `borough` varchar(3) COLLATE utf8_unicode_ci DEFAULT NULL,
  `locality` varchar(2) COLLATE utf8_unicode_ci DEFAULT NULL,
  PRIMARY KEY (`sat_postal_code_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `sat_product_service`
--

DROP TABLE IF EXISTS `sat_product_service`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `sat_product_service` (
  `sat_product_service_id` varchar(8) COLLATE utf8_unicode_ci NOT NULL,
  `description` varchar(256) COLLATE utf8_unicode_ci NOT NULL,
  `keywords` varchar(1024) COLLATE utf8_unicode_ci DEFAULT NULL,
  PRIMARY KEY (`sat_product_service_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `sat_tax_regime`
--

DROP TABLE IF EXISTS `sat_tax_regime`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `sat_tax_regime` (
  `sat_tax_regime_id` varchar(3) COLLATE utf8_unicode_ci NOT NULL,
  `description` varchar(256) COLLATE utf8_unicode_ci NOT NULL,
  PRIMARY KEY (`sat_tax_regime_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `sat_unit_of_measurement`
--

DROP TABLE IF EXISTS `sat_unit_of_measurement`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `sat_unit_of_measurement` (
  `sat_unit_of_measurement_id` varchar(3) COLLATE utf8_unicode_ci NOT NULL,
  `name` varchar(128) COLLATE utf8_unicode_ci NOT NULL,
  `description` varchar(1024) COLLATE utf8_unicode_ci DEFAULT NULL,
  `symbol` varchar(32) COLLATE utf8_unicode_ci DEFAULT NULL,
  PRIMARY KEY (`sat_unit_of_measurement_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `special_receipt`
--

DROP TABLE IF EXISTS `special_receipt`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `special_receipt` (
  `special_receipt_id` int(11) NOT NULL AUTO_INCREMENT,
  `creation_time` datetime NOT NULL,
  `modification_time` datetime NOT NULL,
  `creator` int(11) NOT NULL DEFAULT 0,
  `updater` int(11) NOT NULL DEFAULT 0,
  `salesperson` int(11) NOT NULL DEFAULT 0,
  `store` int(11) NOT NULL DEFAULT 0,
  `serial` int(11) NOT NULL DEFAULT 0,
  `customer` varchar(100) COLLATE utf8_spanish_ci DEFAULT '',
  `ship_to` varchar(100) COLLATE utf8_spanish_ci DEFAULT '',
  `date` datetime NOT NULL DEFAULT '0000-00-00 00:00:00',
  `completed` tinyint(1) NOT NULL DEFAULT 0,
  `cancelled` tinyint(1) NOT NULL DEFAULT 0,
  `delivered` tinyint(1) NOT NULL DEFAULT 0,
  `comment` varchar(500) COLLATE utf8_spanish_ci DEFAULT '',
  `JSON` varchar(1500) COLLATE utf8_spanish_ci DEFAULT '',
  PRIMARY KEY (`special_receipt_id`),
  KEY `FK__employee` (`creator`),
  KEY `FK__employee_2` (`updater`),
  KEY `FK__store` (`store`),
  KEY `FK_special_receipt_employee` (`salesperson`),
  CONSTRAINT `FK__employee` FOREIGN KEY (`creator`) REFERENCES `employee` (`employee_id`),
  CONSTRAINT `FK__employee_2` FOREIGN KEY (`updater`) REFERENCES `employee` (`employee_id`),
  CONSTRAINT `FK__store` FOREIGN KEY (`store`) REFERENCES `store` (`store_id`),
  CONSTRAINT `FK_special_receipt_employee` FOREIGN KEY (`salesperson`) REFERENCES `employee` (`employee_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_spanish_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `store`
--

DROP TABLE IF EXISTS `store`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `store` (
  `store_id` int(11) NOT NULL AUTO_INCREMENT,
  `code` varchar(25) COLLATE utf8_unicode_ci NOT NULL,
  `name` varchar(250) COLLATE utf8_unicode_ci NOT NULL,
  `location` varchar(5) COLLATE utf8_unicode_ci NOT NULL,
  `address` int(11) NOT NULL,
  `taxpayer` varchar(13) COLLATE utf8_unicode_ci NOT NULL,
  `logo` varchar(255) COLLATE utf8_unicode_ci NOT NULL,
  `receipt_message` varchar(250) COLLATE utf8_unicode_ci DEFAULT NULL,
  `default_batch` varchar(10) COLLATE utf8_unicode_ci DEFAULT NULL,
  PRIMARY KEY (`store_id`),
  KEY `store_address_idx` (`address`),
  KEY `store_location_idx` (`location`),
  KEY `store_taxpayer_idx` (`taxpayer`),
  CONSTRAINT `store_address_fk` FOREIGN KEY (`address`) REFERENCES `address` (`address_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `store_location_fk` FOREIGN KEY (`location`) REFERENCES `sat_postal_code` (`sat_postal_code_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `store_taxpayer_fk` FOREIGN KEY (`taxpayer`) REFERENCES `taxpayer_issuer` (`taxpayer_issuer_id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `supplier`
--

DROP TABLE IF EXISTS `supplier`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `supplier` (
  `supplier_id` int(11) NOT NULL AUTO_INCREMENT,
  `code` varchar(25) COLLATE utf8_unicode_ci NOT NULL,
  `name` varchar(250) COLLATE utf8_unicode_ci NOT NULL,
  `zone` varchar(250) COLLATE utf8_unicode_ci DEFAULT NULL,
  `credit_limit` decimal(18,4) NOT NULL,
  `credit_days` int(11) NOT NULL,
  `comment` varchar(500) COLLATE utf8_unicode_ci DEFAULT NULL,
  PRIMARY KEY (`supplier_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `supplier_address`
--

DROP TABLE IF EXISTS `supplier_address`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `supplier_address` (
  `supplier` int(11) NOT NULL,
  `address` int(11) NOT NULL,
  PRIMARY KEY (`supplier`,`address`),
  KEY `supplier_address_supplier_fk_idx` (`supplier`),
  KEY `supplier_adress_address_fk_idx` (`address`),
  CONSTRAINT `supplier_address_supplier_fk` FOREIGN KEY (`supplier`) REFERENCES `supplier` (`supplier_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `supplier_adress_address_fk` FOREIGN KEY (`address`) REFERENCES `address` (`address_id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `supplier_agreement`
--

DROP TABLE IF EXISTS `supplier_agreement`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `supplier_agreement` (
  `supplier_agreement_id` int(11) NOT NULL AUTO_INCREMENT,
  `supplier` int(11) NOT NULL,
  `start` date NOT NULL,
  `end` date NOT NULL,
  `comment` varchar(500) COLLATE utf8_unicode_ci DEFAULT NULL,
  PRIMARY KEY (`supplier_agreement_id`),
  KEY `supplier_agreement_supplier_fk_idx` (`supplier`),
  CONSTRAINT `supplier_agreement_supplier_fk` FOREIGN KEY (`supplier`) REFERENCES `supplier` (`supplier_id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `supplier_bank_account`
--

DROP TABLE IF EXISTS `supplier_bank_account`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `supplier_bank_account` (
  `supplier` int(11) NOT NULL,
  `bank_account` int(11) NOT NULL,
  PRIMARY KEY (`supplier`,`bank_account`),
  KEY `supplier_bank_account_supplier_fk_idx` (`supplier`),
  KEY `supplier_bank_account_bank_account_fk_idx` (`bank_account`),
  CONSTRAINT `supplier_bank_account_bank_account_fk` FOREIGN KEY (`bank_account`) REFERENCES `bank_account` (`bank_account_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `supplier_bank_account_supplier_fk` FOREIGN KEY (`supplier`) REFERENCES `supplier` (`supplier_id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `supplier_contact`
--

DROP TABLE IF EXISTS `supplier_contact`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `supplier_contact` (
  `supplier` int(11) NOT NULL,
  `contact` int(11) NOT NULL,
  PRIMARY KEY (`supplier`,`contact`),
  KEY `supplier_contact_supplier_fk_idx` (`supplier`),
  KEY `supplier_contact_contact_fk_idx` (`contact`),
  CONSTRAINT `supplier_contact_contact_fk` FOREIGN KEY (`contact`) REFERENCES `contact` (`contact_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `supplier_contact_supplier_fk` FOREIGN KEY (`supplier`) REFERENCES `supplier` (`supplier_id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `supplier_payment`
--

DROP TABLE IF EXISTS `supplier_payment`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `supplier_payment` (
  `supplier_payment_id` int(11) NOT NULL AUTO_INCREMENT,
  `supplier` int(11) NOT NULL,
  `amount` decimal(18,4) NOT NULL,
  `method` int(11) NOT NULL,
  `date` datetime NOT NULL,
  `reference` varchar(50) COLLATE utf8_unicode_ci DEFAULT NULL,
  `comment` varchar(500) COLLATE utf8_unicode_ci DEFAULT NULL,
  `creator` int(11) NOT NULL,
  PRIMARY KEY (`supplier_payment_id`),
  KEY `supplier_payment_supplier_fk_idx` (`supplier`),
  KEY `supplier_payment_employee_fk_idx` (`creator`),
  CONSTRAINT `supplier_payment_employee_fk` FOREIGN KEY (`creator`) REFERENCES `employee` (`employee_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `supplier_payment_supplier_fk` FOREIGN KEY (`supplier`) REFERENCES `supplier` (`supplier_id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `supplier_return`
--

DROP TABLE IF EXISTS `supplier_return`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `supplier_return` (
  `supplier_return_id` int(11) NOT NULL AUTO_INCREMENT,
  `purchase_order` int(11) NOT NULL,
  `creator` int(11) NOT NULL,
  `updater` int(11) NOT NULL,
  `supplier` int(11) NOT NULL,
  `creation_time` datetime NOT NULL,
  `modification_time` datetime NOT NULL,
  `completed` tinyint(1) NOT NULL,
  `cancelled` tinyint(1) NOT NULL,
  PRIMARY KEY (`supplier_return_id`),
  KEY `rc_sales_order_fk` (`purchase_order`),
  KEY `rc_creator_fk` (`creator`),
  KEY `rc_updater_fk` (`updater`),
  KEY `sr_supplier_fk_idx` (`supplier`),
  CONSTRAINT `sr_creator_fk` FOREIGN KEY (`creator`) REFERENCES `employee` (`employee_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `sr_purchase_fk` FOREIGN KEY (`purchase_order`) REFERENCES `purchase_order` (`purchase_order_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `sr_supplier_fk` FOREIGN KEY (`supplier`) REFERENCES `supplier` (`supplier_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `sr_updater_fk` FOREIGN KEY (`updater`) REFERENCES `employee` (`employee_id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `supplier_return_detail`
--

DROP TABLE IF EXISTS `supplier_return_detail`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `supplier_return_detail` (
  `supplier_return_detail_id` int(11) NOT NULL AUTO_INCREMENT,
  `supplier_return` int(11) NOT NULL,
  `purchase_order_detail` int(11) NOT NULL,
  `product` int(11) NOT NULL,
  `warehouse` int(11) DEFAULT NULL,
  `quantity` decimal(18,4) NOT NULL,
  `price` decimal(18,4) NOT NULL,
  `product_code` varchar(25) COLLATE utf8_unicode_ci NOT NULL,
  `product_name` varchar(250) COLLATE utf8_unicode_ci NOT NULL,
  `tax_rate` decimal(5,4) NOT NULL,
  `discount` decimal(9,8) NOT NULL,
  `exchange_rate` decimal(8,4) NOT NULL DEFAULT 1.0000,
  `currency` int(11) NOT NULL,
  `tax_included` tinyint(1) NOT NULL,
  PRIMARY KEY (`supplier_return_detail_id`),
  KEY `rcd_sales_order_detail_fk` (`supplier_return`),
  KEY `rcd_product_fk` (`purchase_order_detail`),
  KEY `rcd_return_customer_fk` (`product`),
  KEY `srd_warehouse_fk_idx` (`warehouse`),
  CONSTRAINT `srd_pod_fk` FOREIGN KEY (`purchase_order_detail`) REFERENCES `purchase_order_detail` (`purchase_order_detail_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `srd_product_fk` FOREIGN KEY (`product`) REFERENCES `product` (`product_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `srd_supplier_return_fk` FOREIGN KEY (`supplier_return`) REFERENCES `supplier_return` (`supplier_return_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `srd_warehouse_fk` FOREIGN KEY (`warehouse`) REFERENCES `warehouse` (`warehouse_id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `taxpayer_batch`
--

DROP TABLE IF EXISTS `taxpayer_batch`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `taxpayer_batch` (
  `taxpayer_batch_id` int(11) NOT NULL AUTO_INCREMENT,
  `taxpayer` varchar(13) COLLATE utf8_unicode_ci NOT NULL,
  `batch` varchar(10) COLLATE utf8_unicode_ci NOT NULL,
  `type` int(11) NOT NULL,
  `template` text COLLATE utf8_unicode_ci NOT NULL,
  PRIMARY KEY (`taxpayer_batch_id`),
  UNIQUE KEY `taxpayer_batch_taxpayer_batch_idx` (`taxpayer`,`batch`),
  KEY `taxpayer_batch_taxpayer_idx` (`taxpayer`),
  CONSTRAINT `taxpayer_batch_taxpayer_fk` FOREIGN KEY (`taxpayer`) REFERENCES `taxpayer_issuer` (`taxpayer_issuer_id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `taxpayer_certificate`
--

DROP TABLE IF EXISTS `taxpayer_certificate`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `taxpayer_certificate` (
  `taxpayer_certificate_id` char(20) COLLATE utf8_unicode_ci NOT NULL,
  `taxpayer` varchar(13) COLLATE utf8_unicode_ci NOT NULL,
  `certificate_data` blob NOT NULL,
  `key_data` blob NOT NULL,
  `key_password` tinyblob NOT NULL,
  `valid_from` datetime NOT NULL,
  `valid_to` datetime NOT NULL,
  `active` tinyint(1) NOT NULL,
  PRIMARY KEY (`taxpayer_certificate_id`),
  KEY `taxpayer_certificate_taxpayer_fk_idx` (`taxpayer`),
  CONSTRAINT `taxpayer_certificate_taxpayer_fk` FOREIGN KEY (`taxpayer`) REFERENCES `taxpayer_issuer` (`taxpayer_issuer_id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `taxpayer_issuer`
--

DROP TABLE IF EXISTS `taxpayer_issuer`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `taxpayer_issuer` (
  `taxpayer_issuer_id` varchar(13) COLLATE utf8_unicode_ci NOT NULL,
  `name` varchar(250) COLLATE utf8_unicode_ci DEFAULT NULL,
  `regime` varchar(3) COLLATE utf8_unicode_ci NOT NULL,
  `provider` int(11) NOT NULL,
  `comment` varchar(500) COLLATE utf8_unicode_ci DEFAULT NULL,
  PRIMARY KEY (`taxpayer_issuer_id`),
  KEY `taxpayer_issuer_regime_idx` (`regime`),
  CONSTRAINT `taxpayer_issuer_regime_fk` FOREIGN KEY (`regime`) REFERENCES `sat_tax_regime` (`sat_tax_regime_id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `taxpayer_recipient`
--

DROP TABLE IF EXISTS `taxpayer_recipient`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `taxpayer_recipient` (
  `taxpayer_recipient_id` varchar(13) COLLATE utf8_unicode_ci NOT NULL,
  `name` varchar(250) COLLATE utf8_unicode_ci DEFAULT NULL,
  `email` varchar(80) COLLATE utf8_unicode_ci NOT NULL,
  PRIMARY KEY (`taxpayer_recipient_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `tech_service_receipt`
--

DROP TABLE IF EXISTS `tech_service_receipt`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `tech_service_receipt` (
  `tech_service_receipt_id` int(11) NOT NULL AUTO_INCREMENT,
  `brand` varchar(64) COLLATE utf8_unicode_ci NOT NULL,
  `equipment` varchar(64) COLLATE utf8_unicode_ci NOT NULL,
  `model` varchar(64) COLLATE utf8_unicode_ci NOT NULL,
  `serial_number` varchar(64) COLLATE utf8_unicode_ci DEFAULT NULL,
  `date` datetime NOT NULL,
  `status` varchar(64) COLLATE utf8_unicode_ci DEFAULT NULL,
  `location` varchar(128) COLLATE utf8_unicode_ci DEFAULT NULL,
  `checker` varchar(128) COLLATE utf8_unicode_ci NOT NULL,
  `comment` varchar(1024) COLLATE utf8_unicode_ci DEFAULT NULL,
  PRIMARY KEY (`tech_service_receipt_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `tech_service_receipt_component`
--

DROP TABLE IF EXISTS `tech_service_receipt_component`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `tech_service_receipt_component` (
  `tech_service_receipt_component_id` int(11) NOT NULL AUTO_INCREMENT,
  `receipt` int(11) NOT NULL,
  `name` varchar(128) COLLATE utf8_unicode_ci NOT NULL,
  `quantity` int(11) NOT NULL,
  `serial_number` varchar(64) COLLATE utf8_unicode_ci DEFAULT NULL,
  `comment` varchar(256) COLLATE utf8_unicode_ci DEFAULT NULL,
  PRIMARY KEY (`tech_service_receipt_component_id`),
  KEY `tech_service_receipt_component_receipt_fk_idx` (`receipt`),
  CONSTRAINT `tech_service_receipt_component_receipt_fk` FOREIGN KEY (`receipt`) REFERENCES `tech_service_receipt` (`tech_service_receipt_id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `tech_service_report`
--

DROP TABLE IF EXISTS `tech_service_report`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `tech_service_report` (
  `tech_service_report_id` int(11) NOT NULL AUTO_INCREMENT,
  `date` datetime NOT NULL,
  `location` varchar(128) COLLATE utf8_unicode_ci NOT NULL,
  `type` varchar(128) COLLATE utf8_unicode_ci NOT NULL,
  `equipment` varchar(64) COLLATE utf8_unicode_ci NOT NULL,
  `brand` varchar(64) COLLATE utf8_unicode_ci NOT NULL,
  `model` varchar(64) COLLATE utf8_unicode_ci NOT NULL,
  `serial_number` varchar(64) COLLATE utf8_unicode_ci DEFAULT NULL,
  `user` varchar(128) COLLATE utf8_unicode_ci DEFAULT NULL,
  `technician` varchar(128) COLLATE utf8_unicode_ci DEFAULT NULL,
  `cost` decimal(18,4) NOT NULL,
  `user_report` varchar(1024) COLLATE utf8_unicode_ci DEFAULT NULL,
  `description` varchar(1024) COLLATE utf8_unicode_ci DEFAULT NULL,
  `comment` varchar(1024) COLLATE utf8_unicode_ci DEFAULT NULL,
  PRIMARY KEY (`tech_service_report_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `tech_service_request`
--

DROP TABLE IF EXISTS `tech_service_request`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `tech_service_request` (
  `tech_service_request_id` int(11) NOT NULL AUTO_INCREMENT,
  `type` int(11) NOT NULL,
  `brand` varchar(64) COLLATE utf8_unicode_ci NOT NULL,
  `equipment` varchar(64) COLLATE utf8_unicode_ci NOT NULL,
  `model` varchar(64) COLLATE utf8_unicode_ci NOT NULL,
  `serial_number` varchar(64) COLLATE utf8_unicode_ci DEFAULT NULL,
  `date` datetime NOT NULL,
  `end_date` datetime DEFAULT NULL,
  `customer` int(11) NOT NULL,
  `responsible` varchar(128) COLLATE utf8_unicode_ci NOT NULL,
  `location` varchar(128) COLLATE utf8_unicode_ci NOT NULL,
  `payment_status` varchar(64) COLLATE utf8_unicode_ci DEFAULT NULL,
  `shipping_method` varchar(64) COLLATE utf8_unicode_ci DEFAULT NULL,
  `contact_name` varchar(128) COLLATE utf8_unicode_ci DEFAULT NULL,
  `contact_phone_number` varchar(64) COLLATE utf8_unicode_ci DEFAULT NULL,
  `address` varchar(256) COLLATE utf8_unicode_ci DEFAULT NULL,
  `remarks` varchar(1024) COLLATE utf8_unicode_ci DEFAULT NULL,
  `comment` varchar(1024) COLLATE utf8_unicode_ci DEFAULT NULL,
  PRIMARY KEY (`tech_service_request_id`),
  KEY `tech_service_request_customer_idx` (`customer`),
  CONSTRAINT `tech_service_request_customer_fk` FOREIGN KEY (`customer`) REFERENCES `customer` (`customer_id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `tech_service_request_component`
--

DROP TABLE IF EXISTS `tech_service_request_component`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `tech_service_request_component` (
  `tech_service_request_component_id` int(11) NOT NULL AUTO_INCREMENT,
  `request` int(11) NOT NULL,
  `name` varchar(128) COLLATE utf8_unicode_ci NOT NULL,
  `quantity` int(11) NOT NULL,
  `serial_number` varchar(64) COLLATE utf8_unicode_ci DEFAULT NULL,
  `comment` varchar(256) COLLATE utf8_unicode_ci DEFAULT NULL,
  PRIMARY KEY (`tech_service_request_component_id`),
  KEY `tech_service_request_component_request_idx` (`request`),
  CONSTRAINT `tech_service_request_component_request_fk` FOREIGN KEY (`request`) REFERENCES `tech_service_request` (`tech_service_request_id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `translation_request`
--

DROP TABLE IF EXISTS `translation_request`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `translation_request` (
  `translation_request_id` int(11) NOT NULL AUTO_INCREMENT,
  `requester` int(11) NOT NULL,
  `date` datetime NOT NULL,
  `agency` varchar(256) COLLATE utf8_unicode_ci NOT NULL,
  `document_name` varchar(128) COLLATE utf8_unicode_ci NOT NULL,
  `amount` decimal(18,4) NOT NULL,
  `delivery_date` datetime NOT NULL,
  `comment` varchar(1024) COLLATE utf8_unicode_ci DEFAULT NULL,
  PRIMARY KEY (`translation_request_id`),
  KEY `translation_request_requester_idx` (`requester`),
  CONSTRAINT `translation_request_requester_fk` FOREIGN KEY (`requester`) REFERENCES `employee` (`employee_id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `user`
--

DROP TABLE IF EXISTS `user`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `user` (
  `user_id` varchar(20) COLLATE utf8_unicode_ci NOT NULL,
  `password` varchar(40) COLLATE utf8_unicode_ci NOT NULL,
  `email` varchar(250) COLLATE utf8_unicode_ci NOT NULL,
  `employee` int(11) DEFAULT NULL,
  `administrator` bit(1) NOT NULL,
  PRIMARY KEY (`user_id`),
  KEY `employee_user_idx` (`employee`),
  CONSTRAINT `employee_user` FOREIGN KEY (`employee`) REFERENCES `employee` (`employee_id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `warehouse`
--

DROP TABLE IF EXISTS `warehouse`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `warehouse` (
  `warehouse_id` int(11) NOT NULL AUTO_INCREMENT,
  `store` int(11) NOT NULL,
  `code` varchar(25) COLLATE utf8_unicode_ci NOT NULL,
  `name` varchar(250) COLLATE utf8_unicode_ci NOT NULL,
  `comment` varchar(500) COLLATE utf8_unicode_ci DEFAULT NULL,
  PRIMARY KEY (`warehouse_id`),
  UNIQUE KEY `code_UNIQUE` (`code`),
  KEY `warehouse_store_fk_idx` (`store`),
  CONSTRAINT `warehouse_store_fk` FOREIGN KEY (`store`) REFERENCES `store` (`store_id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2021-03-22 11:53:30
