module.exports={
  testDir:__dirname,
  testMatch:'prototype-audit.spec.js',
  workers:1,
  reporter:'line',
  use:{trace:'retain-on-failure'},
};
