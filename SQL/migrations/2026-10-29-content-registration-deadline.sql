-- Apply once to existing GamMaSite MySQL databases.
ALTER TABLE `ContentItems`
  ADD COLUMN `RegistrationDeadline` datetime DEFAULT NULL AFTER `EndDate`;
