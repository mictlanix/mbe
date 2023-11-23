CREATE TABLE `user_settings` (
  `user` varchar(20) COLLATE utf8_unicode_ci NOT NULL,
  `store` int(11) NOT NULL,
  `point_sale` int(11) NOT NULL,
  `cash_drawer` int(11) DEFAULT NULL,
  PRIMARY KEY (`user`),
  KEY `user_settings_user_fk_idx` (`user`),
  KEY `user_settings_store_fk_idx` (`store`),
  KEY `user_settings_point_sale_fk_idx` (`point_sale`),
  KEY `user_settings_cash_drawer_fk_idx` (`cash_drawer`),
  CONSTRAINT `user_settings_cash_drawer_fk` FOREIGN KEY (`cash_drawer`) REFERENCES `cash_drawer` (`cash_drawer_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `user_settings_point_sale_fk` FOREIGN KEY (`point_sale`) REFERENCES `point_sale` (`point_sale_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `user_settings_store_fk` FOREIGN KEY (`store`) REFERENCES `store` (`store_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `user_settings_user_fk` FOREIGN KEY (`user`) REFERENCES `user` (`user_id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
