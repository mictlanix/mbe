CREATE TABLE `user_settings` (
  `user` varchar(20) NOT NULL,
  `store` int(11) NOT NULL,
  `point_sale` int(11) NOT NULL,
  `cash_drawer` int(11) NOT NULL,
  PRIMARY KEY (`user`),
  KEY `user_settings_user_fk_idx` (`user`),
  KEY `user_settings_store_fk_idx` (`store`),
  KEY `user_settings_point_sale_fk_idx` (`point_sale`),
  KEY `user_settings_cash_drawer_fk_idx` (`cash_drawer`),
  CONSTRAINT `user_settings_user_fk` FOREIGN KEY (`user`) REFERENCES `user` (`user_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `user_settings_store_fk` FOREIGN KEY (`store`) REFERENCES `store` (`store_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `user_settings_point_sale_fk` FOREIGN KEY (`point_sale`) REFERENCES `point_sale` (`point_sale_id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `user_settings_cash_drawer_fk` FOREIGN KEY (`cash_drawer`) REFERENCES `cash_drawer` (`cash_drawer_id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;

INSERT INTO `user_settings` VALUE
('achuicalco', '8', '13', '9'),
('admin', '1', '1', '4'),
('andres', '1', '1', '4'),
('blockera', '1', '1', '4'),
('concretosrh', '47', '14', '10'),
('contabilidad', '1', '1', '4'),
('coyotepec', '1', '1', '4'),
('cristian', '1', '1', '4'),
('distribuidora', '51', '18', '14'),
('ferreteria', '1', '1', '4'),
('ferrnando', '1', '1', '4'),
('gruporamos', '50', '17', '13'),
('huehuetoca', '1', '1', '4'),
('laura', '1', '1', '4'),
('mauricio', '1', '1', '4'),
('raga84', '1', '1', '4');
